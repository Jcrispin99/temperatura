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
    public DateOnly? FechaDesde { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? FechaHasta { get; set; }

    [BindProperty(SupportsGet = true)]
    public int[]? AmbienteIds { get; set; }

    public IReadOnlyList<AmbienteOpcion> Ambientes { get; private set; } = [];
    public IReadOnlyList<GraficaMedicion> Graficas { get; private set; } = [];
    public IReadOnlyList<GrupoObservacionesAmbiente> ObservacionesPorAmbiente { get; private set; } = [];
    public string DatosGraficasJson { get; private set; } = "[]";
    public string TituloPeriodo { get; private set; } = string.Empty;
    public int CantidadColumnas { get; private set; }
    public int MaximoDias => EjeGraficaLecturas.MaximoDias;
    public bool EsSupervisor => User.IsInRole("Supervisor");

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(_ventanaRegistroService.ObtenerAhoraLocal().DateTime);
        FechaHasta ??= hoy;
        FechaDesde ??= FechaHasta.Value.AddDays(-6);

        ValidarRangoFechas();
        TituloPeriodo = $"{FechaDesde.Value:dd/MM/yyyy} al {FechaHasta.Value:dd/MM/yyyy}";

        var usuarioId = _userManager.GetUserId(User)!;
        Ambientes = await ObtenerAmbientesAutorizadosAsync(usuarioId, cancellationToken);

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
            .Select(x => new GraficaMedicion(x.Id, x.Nombre, x.Unidad, [], []))
            .ToList();

        if (!ModelState.IsValid || idsSeleccionados.Length == 0)
        {
            PrepararJson();
            return;
        }

        var etiquetas = EjeGraficaLecturas.Crear(FechaDesde.Value, FechaHasta.Value);
        CantidadColumnas = etiquetas.Count;

        var filas = await _context.DetallesRegistro
            .AsNoTracking()
            .Where(x =>
                idsSeleccionados.Contains(x.Registro.AmbienteId) &&
                x.Registro.Estado == EstadoRegistro.Confirmado &&
                x.Registro.FechaOperativa >= FechaDesde.Value &&
                x.Registro.FechaOperativa <= FechaHasta.Value)
            .OrderBy(x => x.Registro.FechaOperativa)
            .ThenBy(x => x.Registro.MomentoOperativoAplicado)
            .ThenBy(x => x.Registro.HoraReferenciaAplicada)
            .ThenBy(x => x.Registro.Ambiente.Nombre)
            .Select(x => new FilaMedicion(
                x.RegistroId,
                x.TipoMedicionId,
                x.Registro.AmbienteId,
                x.Registro.Ambiente.Nombre,
                x.Registro.FechaOperativa,
                x.Registro.HorarioNombreAplicado,
                x.Registro.HoraReferenciaAplicada,
                x.Registro.MomentoOperativoAplicado,
                x.Registro.EsCierreDiaOperativoAnteriorAplicado,
                x.Valor,
                x.LimiteMinimoAplicado,
                x.LimiteMaximoAplicado,
                x.EstadoRango,
                x.Observacion))
            .ToListAsync(cancellationToken);

        if (TieneMomentosDuplicados(filas))
        {
            ModelState.AddModelError(
                string.Empty,
                "Hay más de una lectura para el mismo ambiente, día y momento operativo. Revisa la configuración de horarios.");
            PrepararJson();
            return;
        }

        Graficas = tipos
            .Select(tipo => new GraficaMedicion(
                tipo.Id,
                tipo.Nombre,
                tipo.Unidad,
                etiquetas,
                CrearPuntos(filas.Where(x => x.TipoMedicionId == tipo.Id))))
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

    private void ValidarRangoFechas()
    {
        if (FechaDesde is not { } fechaDesde || FechaHasta is not { } fechaHasta)
        {
            ModelState.AddModelError(string.Empty, "Selecciona ambas fechas del rango.");
            return;
        }

        if (fechaDesde > fechaHasta)
        {
            ModelState.AddModelError(string.Empty, "La fecha inicial no puede ser posterior a la fecha final.");
            return;
        }

        var cantidadDias = fechaHasta.DayNumber - fechaDesde.DayNumber + 1;
        if (cantidadDias > EjeGraficaLecturas.MaximoDias)
        {
            ModelState.AddModelError(
                string.Empty,
                $"Selecciona un rango máximo de {EjeGraficaLecturas.MaximoDias} días.");
        }
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

    private static bool TieneMomentosDuplicados(IEnumerable<FilaMedicion> filas) =>
        filas
            .GroupBy(x => new
            {
                x.TipoMedicionId,
                x.AmbienteId,
                x.FechaOperativa,
                x.MomentoOperativo
            })
            .Any(x => x.Count() > 1);

    private static IReadOnlyList<PuntoGrafica> CrearPuntos(IEnumerable<FilaMedicion> filas) =>
        filas
            .Select(fila => new PuntoGrafica(
                fila.AmbienteId,
                fila.Ambiente,
                EjeGraficaLecturas.CrearClave(fila.FechaOperativa, fila.MomentoOperativo),
                fila.FechaOperativa.ToString("dd/MM/yyyy"),
                fila.MomentoOperativo.ObtenerNombre(),
                fila.Horario,
                fila.HoraReferencia.ToString("HH\\:mm"),
                fila.EsCierreDiaOperativoAnterior,
                fila.Valor,
                fila.EstadoRango.ToString(),
                fila.LimiteMinimo,
                fila.LimiteMaximo,
                ObtenerColorAmbiente(fila.Ambiente, fila.AmbienteId)))
            .OrderBy(x => x.ClaveEje)
            .ThenBy(x => x.Ambiente)
            .ToList();

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
        string Horario,
        TimeOnly HoraReferencia,
        MomentoOperativo MomentoOperativo,
        bool EsCierreDiaOperativoAnterior,
        decimal Valor,
        decimal LimiteMinimo,
        decimal LimiteMaximo,
        EstadoRango EstadoRango,
        string? Observacion);

    public sealed record AmbienteOpcion(int Id, string Nombre, bool Activo);

    public sealed record GraficaMedicion(
        int TipoMedicionId,
        string Nombre,
        string Unidad,
        IReadOnlyList<ColumnaGraficaLectura> Etiquetas,
        IReadOnlyList<PuntoGrafica> Puntos);

    public sealed record PuntoGrafica(
        int AmbienteId,
        string Ambiente,
        string ClaveEje,
        string FechaOperativa,
        string Momento,
        string Horario,
        string HoraReferencia,
        bool EsDiaSiguiente,
        decimal Valor,
        string EstadoRango,
        decimal LimiteMinimo,
        decimal LimiteMaximo,
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
