using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Data;
using Temperatura.Web.Domain;
using Temperatura.Web.Domain.Enums;
using Temperatura.Web.Services;

namespace Temperatura.Web.Pages.Graficas;

[Authorize]
public class IndexModel(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IVentanaRegistroService ventanaRegistroService) : PageModel
{
    private static readonly CultureInfo CulturaEspanol = CultureInfo.GetCultureInfo("es-PE");
    private static readonly string[] ColoresAlternativos =
    [
        "#17a2b8",
        "#6f42c1",
        "#0ca678",
        "#fd7e14",
        "#4263eb",
        "#e8590c",
        "#5f3dc4"
    ];
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IVentanaRegistroService _ventanaRegistroService = ventanaRegistroService;

    [BindProperty(SupportsGet = true)]
    public PeriodoDashboard Periodo { get; set; } = PeriodoDashboard.Semanal;

    [BindProperty(SupportsGet = true)]
    public ModoVisualizacionGrafica Modo { get; set; } = ModoVisualizacionGrafica.PromedioDiario;

    [BindProperty(SupportsGet = true)]
    public DateOnly? Fecha { get; set; }

    [BindProperty(SupportsGet = true)]
    public int[]? AmbienteIds { get; set; }

    public IReadOnlyList<AmbienteOpcion> Ambientes { get; private set; } = [];
    public IReadOnlyList<HorarioOpcion> Horarios { get; private set; } = [];
    public IReadOnlyList<GraficaMedicion> Graficas { get; private set; } = [];
    public IReadOnlyList<GrupoObservacionesAmbiente> ObservacionesPorAmbiente { get; private set; } = [];
    public string DatosGraficasJson { get; private set; } = "[]";
    public DateOnly FechaDesde { get; private set; }
    public DateOnly FechaHasta { get; private set; }
    public string TituloPeriodo { get; private set; } = string.Empty;
    public bool EsSupervisor => User.IsInRole("Supervisor");

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(_ventanaRegistroService.ObtenerAhoraLocal().DateTime);
        Fecha ??= hoy;
        if (Periodo == PeriodoDashboard.Diario)
        {
            Modo = ModoVisualizacionGrafica.DetalleHorarios;
            Periodo = PeriodoDashboard.Semanal;
        }

        if (Modo is not ModoVisualizacionGrafica.PromedioDiario and
            not ModoVisualizacionGrafica.DetalleHorarios)
        {
            ModelState.AddModelError(string.Empty, "Selecciona un modo de visualización válido.");
            Modo = ModoVisualizacionGrafica.PromedioDiario;
        }

        if (Periodo is not PeriodoDashboard.Semanal and not PeriodoDashboard.Mensual)
        {
            ModelState.AddModelError(string.Empty, "Selecciona un período semanal o mensual válido.");
            Periodo = PeriodoDashboard.Semanal;
        }

        (FechaDesde, FechaHasta) = Modo == ModoVisualizacionGrafica.DetalleHorarios
            ? (Fecha.Value, Fecha.Value)
            : ObtenerRangoPeriodo(Periodo, Fecha.Value);
        TituloPeriodo = Modo == ModoVisualizacionGrafica.DetalleHorarios
            ? ObtenerTituloDia(Fecha.Value)
            : ObtenerTituloPeriodo(Periodo, Fecha.Value, FechaDesde);

        var usuarioId = _userManager.GetUserId(User)!;
        Ambientes = await ObtenerAmbientesAutorizadosAsync(usuarioId, cancellationToken);
        Horarios = await _context.Horarios
            .AsNoTracking()
            .OrderBy(x => x.EsCierreDiaOperativoAnterior)
            .ThenBy(x => x.HoraReferencia)
            .Select(x => new HorarioOpcion(
                x.Id,
                x.Nombre,
                x.HoraReferencia,
                x.Activo,
                x.EsCierreDiaOperativoAnterior))
            .ToListAsync(cancellationToken);

        var idsAutorizados = Ambientes.Select(x => x.Id).ToHashSet();
        var idsSolicitados = AmbienteIds?
            .Distinct()
            .ToArray() ?? [];

        if (idsSolicitados.Any(x => !idsAutorizados.Contains(x)))
        {
            ModelState.AddModelError(string.Empty, "Uno o más ambientes seleccionados no están disponibles para tu usuario.");
        }

        var idsSeleccionados = idsSolicitados
            .Where(idsAutorizados.Contains)
            .ToArray();
        if (idsSeleccionados.Length == 0)
        {
            idsSeleccionados = idsAutorizados.ToArray();
        }

        AmbienteIds = idsSeleccionados;

        var tipos = await _context.TiposMedicion
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new TipoGrafica(x.Id, x.Nombre, x.SimboloUnidad))
            .ToListAsync(cancellationToken);

        Graficas = tipos
            .Select(x => new GraficaMedicion(
                x.Id,
                x.Nombre,
                x.Unidad,
                Modo == ModoVisualizacionGrafica.PromedioDiario,
                [],
                []))
            .ToList();

        if (!ModelState.IsValid || idsSeleccionados.Length == 0)
        {
            PrepararJson();
            return;
        }

        var configuracionesHorario = await _context.AmbientesHorarios
            .AsNoTracking()
            .Where(x =>
                idsSeleccionados.Contains(x.AmbienteId) &&
                x.VigenteDesde <= FechaHasta &&
                (x.VigenteHasta == null || x.VigenteHasta >= FechaDesde))
            .Select(x => new ConfiguracionHorarioFila(
                x.Id,
                x.AmbienteId,
                x.HorarioId,
                x.VigenteDesde,
                x.VigenteHasta,
                x.Activo))
            .ToListAsync(cancellationToken);

        var consulta = _context.DetallesRegistro
            .AsNoTracking()
            .Where(x =>
                idsSeleccionados.Contains(x.Registro.AmbienteId) &&
                x.Registro.Estado == EstadoRegistro.Confirmado &&
                x.Registro.FechaOperativa >= FechaDesde &&
                x.Registro.FechaOperativa <= FechaHasta);

        var filas = await consulta
            .OrderBy(x => x.Registro.FechaOperativa)
            .ThenBy(x => x.Registro.Horario.EsCierreDiaOperativoAnterior)
            .ThenBy(x => x.Registro.Horario.HoraReferencia)
            .ThenBy(x => x.Registro.Ambiente.Nombre)
            .Select(x => new FilaMedicion(
                x.RegistroId,
                x.TipoMedicionId,
                x.Registro.AmbienteId,
                x.Registro.Ambiente.Nombre,
                x.Registro.FechaOperativa,
                x.Registro.HorarioId,
                x.Registro.Horario.Nombre,
                x.Registro.Horario.HoraReferencia,
                x.Registro.Horario.EsCierreDiaOperativoAnterior,
                x.Valor,
                x.LimiteMinimoAplicado,
                x.LimiteMaximoAplicado,
                x.EstadoRango,
                x.Observacion))
            .ToListAsync(cancellationToken);

        var etiquetas = CrearEtiquetasEje(Modo, Periodo, FechaDesde, FechaHasta, Horarios);
        var cantidadesEsperadas = CrearCantidadesEsperadas(
            idsSeleccionados,
            FechaDesde,
            FechaHasta,
            configuracionesHorario);

        Graficas = tipos
            .Select(tipo => new GraficaMedicion(
                tipo.Id,
                tipo.Nombre,
                tipo.Unidad,
                Modo == ModoVisualizacionGrafica.PromedioDiario,
                etiquetas,
                CrearPuntos(
                    Modo,
                    filas.Where(x => x.TipoMedicionId == tipo.Id).ToList(),
                    cantidadesEsperadas)))
            .ToList();

        var tiposPorId = tipos.ToDictionary(x => x.Id);
        ObservacionesPorAmbiente = filas
            .Where(x => !string.IsNullOrWhiteSpace(x.Observacion))
            .GroupBy(x => new { x.AmbienteId, x.Ambiente })
            .OrderBy(x => x.Key.Ambiente)
            .Select(grupo => new GrupoObservacionesAmbiente(
                grupo.Key.AmbienteId,
                grupo.Key.Ambiente,
                ObtenerColorAmbiente(grupo.Key.Ambiente, grupo.Key.AmbienteId),
                grupo
                    .Select(x => CrearObservacion(x, tiposPorId[x.TipoMedicionId]))
                    .OrderByDescending(x => x.FechaHora)
                    .ThenBy(x => x.Medicion)
                    .ToList()))
            .ToList();

        PrepararJson();
    }

    private static (DateOnly Desde, DateOnly Hasta) ObtenerRangoPeriodo(
        PeriodoDashboard periodo,
        DateOnly fechaReferencia)
    {
        if (periodo == PeriodoDashboard.Diario)
        {
            return (fechaReferencia, fechaReferencia);
        }

        if (periodo == PeriodoDashboard.Mensual)
        {
            var primerDia = new DateOnly(fechaReferencia.Year, fechaReferencia.Month, 1);
            return (primerDia, primerDia.AddMonths(1).AddDays(-1));
        }

        var diasDesdeLunes = ((int)fechaReferencia.DayOfWeek + 6) % 7;
        var lunes = fechaReferencia.AddDays(-diasDesdeLunes);
        return (lunes, lunes.AddDays(6));
    }

    private static string ObtenerTituloPeriodo(
        PeriodoDashboard periodo,
        DateOnly fechaReferencia,
        DateOnly fechaDesde)
    {
        if (periodo == PeriodoDashboard.Semanal)
        {
            var fechaIso = fechaDesde.ToDateTime(TimeOnly.MinValue);
            return $"Semana {ISOWeek.GetWeekOfYear(fechaIso)} de {ISOWeek.GetYear(fechaIso)}";
        }

        var nombreMes = fechaReferencia
            .ToDateTime(TimeOnly.MinValue)
            .ToString("MMMM", CulturaEspanol);
        return $"{CulturaEspanol.TextInfo.ToTitleCase(nombreMes)} de {fechaReferencia.Year}";
    }

    private static string ObtenerTituloDia(DateOnly fecha)
    {
        var titulo = fecha
            .ToDateTime(TimeOnly.MinValue)
            .ToString("dddd d 'de' MMMM 'de' yyyy", CulturaEspanol);
        return char.ToUpper(titulo[0], CulturaEspanol) + titulo[1..];
    }

    private async Task<IReadOnlyList<AmbienteOpcion>> ObtenerAmbientesAutorizadosAsync(
        string usuarioId,
        CancellationToken cancellationToken)
    {
        if (EsSupervisor)
        {
            return await _context.Ambientes
                .AsNoTracking()
                .OrderByDescending(x => x.Activo)
                .ThenBy(x => x.Nombre)
                .Select(x => new AmbienteOpcion(x.Id, x.Nombre, x.Activo))
                .ToListAsync(cancellationToken);
        }

        return await _context.UsuariosAmbientes
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.Activo)
            .OrderByDescending(x => x.Ambiente.Activo)
            .ThenBy(x => x.Ambiente.Nombre)
            .Select(x => new AmbienteOpcion(x.AmbienteId, x.Ambiente.Nombre, x.Ambiente.Activo))
            .ToListAsync(cancellationToken);
    }

    private void PrepararJson()
    {
        DatosGraficasJson = JsonSerializer.Serialize(Graficas, OpcionesJson);
    }

    private static IReadOnlyList<EtiquetaEjeGrafica> CrearEtiquetasEje(
        ModoVisualizacionGrafica modo,
        PeriodoDashboard periodo,
        DateOnly fechaDesde,
        DateOnly fechaHasta,
        IReadOnlyList<HorarioOpcion> horarios)
    {
        if (modo == ModoVisualizacionGrafica.DetalleHorarios)
        {
            return horarios
                .Select(x => new EtiquetaEjeGrafica(
                    x.Id.ToString(CultureInfo.InvariantCulture),
                    ObtenerNombreMomentoDia(x)))
                .ToList();
        }

        var etiquetas = new List<EtiquetaEjeGrafica>();
        for (var fecha = fechaDesde; fecha <= fechaHasta; fecha = fecha.AddDays(1))
        {
            var etiqueta = periodo == PeriodoDashboard.Semanal
                ? fecha.ToDateTime(TimeOnly.MinValue).ToString("ddd dd/MM", CulturaEspanol)
                : fecha.ToString("dd/MM", CultureInfo.InvariantCulture);
            etiquetas.Add(new EtiquetaEjeGrafica(
                fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                etiqueta));
        }

        return etiquetas;
    }

    private static string ObtenerNombreMomentoDia(HorarioOpcion horario)
    {
        if (horario.EsCierreDiaOperativoAnterior || horario.HoraReferencia.Hour < 5)
        {
            return "Medianoche";
        }

        if (horario.HoraReferencia.Hour < 12)
        {
            return "Mañana";
        }

        return horario.HoraReferencia.Hour < 18 ? "Tarde" : "Noche";
    }

    private static IReadOnlyDictionary<(int AmbienteId, DateOnly Fecha), int> CrearCantidadesEsperadas(
        IReadOnlyCollection<int> ambienteIds,
        DateOnly fechaDesde,
        DateOnly fechaHasta,
        IReadOnlyCollection<ConfiguracionHorarioFila> configuraciones)
    {
        var resultado = new Dictionary<(int AmbienteId, DateOnly Fecha), int>();
        foreach (var ambienteId in ambienteIds)
        {
            for (var fecha = fechaDesde; fecha <= fechaHasta; fecha = fecha.AddDays(1))
            {
                var cantidad = configuraciones
                    .Where(x =>
                        x.AmbienteId == ambienteId &&
                        x.VigenteDesde <= fecha &&
                        (x.VigenteHasta == null || x.VigenteHasta >= fecha))
                    .GroupBy(x => x.HorarioId)
                    .Select(x => x
                        .OrderByDescending(y => y.Activo)
                        .ThenByDescending(y => y.VigenteDesde)
                        .ThenByDescending(y => y.Id)
                        .First())
                    .Count();
                resultado[(ambienteId, fecha)] = cantidad;
            }
        }

        return resultado;
    }

    private static IReadOnlyList<PuntoGrafica> CrearPuntos(
        ModoVisualizacionGrafica modo,
        IReadOnlyCollection<FilaMedicion> filas,
        IReadOnlyDictionary<(int AmbienteId, DateOnly Fecha), int> cantidadesEsperadas)
    {
        if (modo == ModoVisualizacionGrafica.DetalleHorarios)
        {
            return filas
                .Select(fila => new PuntoGrafica(
                    fila.AmbienteId,
                    fila.Ambiente,
                    fila.HorarioId.ToString(CultureInfo.InvariantCulture),
                    fila.FechaOperativa.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    fila.Horario,
                    fila.Valor,
                    fila.Valor,
                    fila.Valor,
                    1,
                    1,
                    fila.EstadoRango == EstadoRango.DentroDeRango ? 0 : 1,
                    fila.LimiteMinimo,
                    fila.LimiteMaximo,
                    false,
                    ObtenerColorAmbiente(fila.Ambiente, fila.AmbienteId)))
                .OrderBy(x => x.ClaveEje)
                .ThenBy(x => x.Ambiente)
                .ToList();
        }

        return filas
            .GroupBy(x => new
            {
                x.AmbienteId,
                x.Ambiente,
                x.FechaOperativa
            })
            .Select(grupo =>
            {
                var cantidadRegistros = grupo.Count();
                var cantidadEsperada = cantidadesEsperadas.GetValueOrDefault(
                    (grupo.Key.AmbienteId, grupo.Key.FechaOperativa),
                    cantidadRegistros);
                if (cantidadEsperada == 0)
                {
                    cantidadEsperada = cantidadRegistros;
                }

                return new PuntoGrafica(
                    grupo.Key.AmbienteId,
                    grupo.Key.Ambiente,
                    grupo.Key.FechaOperativa.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    grupo.Key.FechaOperativa.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    null,
                    Math.Round(grupo.Average(x => x.Valor), 2),
                    grupo.Min(x => x.Valor),
                    grupo.Max(x => x.Valor),
                    cantidadRegistros,
                    cantidadEsperada,
                    grupo.Count(x => x.EstadoRango != EstadoRango.DentroDeRango),
                    grupo.Min(x => x.LimiteMinimo),
                    grupo.Max(x => x.LimiteMaximo),
                    true,
                    ObtenerColorAmbiente(grupo.Key.Ambiente, grupo.Key.AmbienteId));
            })
            .OrderBy(x => x.ClaveEje)
            .ThenBy(x => x.Ambiente)
            .ToList();
    }

    private static ObservacionGrafica CrearObservacion(FilaMedicion fila, TipoGrafica tipo) =>
        new(
            fila.RegistroId,
            tipo.Id,
            tipo.Nombre,
            tipo.Unidad,
            ObtenerFechaHoraProgramada(fila),
            fila.FechaOperativa,
            fila.Horario,
            fila.Valor,
            fila.LimiteMinimo,
            fila.LimiteMaximo,
            fila.EstadoRango,
            fila.Observacion!.Trim());

    public IReadOnlyList<GrupoObservacionesAmbiente> ObtenerObservacionesPorTipo(int tipoMedicionId) =>
        ObservacionesPorAmbiente
            .Select(grupo => grupo with
            {
                Observaciones = grupo.Observaciones
                    .Where(x => x.TipoMedicionId == tipoMedicionId)
                    .ToList()
            })
            .Where(x => x.Observaciones.Count > 0)
            .ToList();

    private static DateTime ObtenerFechaHoraProgramada(FilaMedicion fila)
    {
        var fechaCalendario = fila.EsCierreDiaOperativoAnterior
            ? fila.FechaOperativa.AddDays(1)
            : fila.FechaOperativa;
        return fechaCalendario.ToDateTime(fila.HoraReferencia);
    }

    private static string ObtenerColorAmbiente(string nombre, int ambienteId)
    {
        var nombreNormalizado = nombre.Trim().ToUpperInvariant();
        return nombreNormalizado switch
        {
            "UMA 1" => "#f59f00",
            "UMA 2" => "#206bc4",
            "UMA 3" => "#ae3ec9",
            "FARMACIA" or "ALMACEN FARMACIA" or "ALMACÉN FARMACIA" or
                "ALMACEN DE FARMACIA" or "ALMACÉN DE FARMACIA" => "#2fb344",
            "ENFERMERIA" or "ENFERMERÍA" => "#d63939",
            _ => ColoresAlternativos[Math.Abs(ambienteId) % ColoresAlternativos.Length]
        };
    }

    private sealed record TipoGrafica(int Id, string Nombre, string Unidad);

    private sealed record FilaMedicion(
        long RegistroId,
        int TipoMedicionId,
        int AmbienteId,
        string Ambiente,
        DateOnly FechaOperativa,
        int HorarioId,
        string Horario,
        TimeOnly HoraReferencia,
        bool EsCierreDiaOperativoAnterior,
        decimal Valor,
        decimal LimiteMinimo,
        decimal LimiteMaximo,
        EstadoRango EstadoRango,
        string? Observacion);

    private sealed record ConfiguracionHorarioFila(
        int Id,
        int AmbienteId,
        int HorarioId,
        DateOnly VigenteDesde,
        DateOnly? VigenteHasta,
        bool Activo);

    public sealed record AmbienteOpcion(int Id, string Nombre, bool Activo);
    public sealed record HorarioOpcion(
        int Id,
        string Nombre,
        TimeOnly HoraReferencia,
        bool Activo,
        bool EsCierreDiaOperativoAnterior);

    public sealed record GraficaMedicion(
        int TipoMedicionId,
        string Nombre,
        string Unidad,
        bool EsPromedioDiario,
        IReadOnlyList<EtiquetaEjeGrafica> Etiquetas,
        IReadOnlyList<PuntoGrafica> Puntos);

    public sealed record EtiquetaEjeGrafica(string Clave, string Etiqueta);

    public sealed record PuntoGrafica(
        int AmbienteId,
        string Ambiente,
        string ClaveEje,
        string FechaOperativa,
        string? Horario,
        decimal Valor,
        decimal ValorMinimo,
        decimal ValorMaximo,
        int CantidadRegistros,
        int CantidadEsperada,
        int CantidadFueraDeRango,
        decimal LimiteMinimo,
        decimal LimiteMaximo,
        bool EsPromedio,
        string ColorAmbiente);

    public sealed record GrupoObservacionesAmbiente(
        int AmbienteId,
        string Ambiente,
        string Color,
        IReadOnlyList<ObservacionGrafica> Observaciones);

    public sealed record ObservacionGrafica(
        long RegistroId,
        int TipoMedicionId,
        string Medicion,
        string Unidad,
        DateTime FechaHora,
        DateOnly FechaOperativa,
        string Horario,
        decimal Valor,
        decimal LimiteMinimo,
        decimal LimiteMaximo,
        EstadoRango EstadoRango,
        string Observacion);
}

public enum ModoVisualizacionGrafica
{
    PromedioDiario = 1,
    DetalleHorarios = 2
}
