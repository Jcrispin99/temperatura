using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Data;
using Temperatura.Web.Domain;
using Temperatura.Web.Domain.Enums;
using Temperatura.Web.Services;

namespace Temperatura.Web.Pages.Registros;

[Authorize]
public class NuevoModel(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IVentanaRegistroService ventanaRegistroService,
    ILogger<NuevoModel> logger) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IVentanaRegistroService _ventanaRegistroService = ventanaRegistroService;
    private readonly ILogger<NuevoModel> _logger = logger;

    [BindProperty(SupportsGet = true)]
    public int? AmbienteId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int HorarioId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? FechaOperativaSeleccionada { get; set; }

    [BindProperty]
    public List<MedicionInput> Mediciones { get; set; } = [];

    [BindProperty]
    public bool ConfirmacionFueraDeRango { get; set; }

    [BindProperty]
    public string? MotivoFueraDePlazo { get; set; }

    public IReadOnlyList<AmbienteOpcion> Ambientes { get; private set; } = [];

    public IReadOnlyList<HorarioOpcion> HorariosDisponibles { get; private set; } = [];

    public IReadOnlyList<HorarioOpcion> RondasPendientesRegularizacion => HorariosDisponibles
        .Where(x => x.Puntualidad == EstadoPuntualidad.FueraDePlazo)
        .OrderByDescending(x => x.FechaOperativa)
        .ThenByDescending(x => x.Cierre)
        .ToArray();

    public IReadOnlyList<HorarioOpcion> RondasActualesDisponibles => HorariosDisponibles
        .Where(x => x.Puntualidad != EstadoPuntualidad.FueraDePlazo)
        .OrderBy(x => x.Apertura)
        .ToArray();

    public HorarioOpcion? HorarioSeleccionado { get; private set; }

    public string? AmbienteSeleccionado { get; private set; }

    public string? MensajeSinVentana { get; private set; }

    [TempData]
    public string? MensajeExito { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var preparado = await PrepararPaginaAsync(
            null,
            null,
            permitirAmbientePredeterminado: true);
        return preparado ? Page() : Forbid();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var medicionesEnviadas = Mediciones.ToArray();
        ModelState.Clear();

        var valoresPorConfiguracion = medicionesEnviadas
            .GroupBy(x => x.AmbienteMedicionId)
            .ToDictionary(x => x.Key, x => x.First().Valor);
        var observacionesPorConfiguracion = medicionesEnviadas
            .GroupBy(x => x.AmbienteMedicionId)
            .ToDictionary(x => x.Key, x => x.First().Observacion);

        if (medicionesEnviadas.GroupBy(x => x.AmbienteMedicionId).Any(x => x.Count() > 1))
        {
            ModelState.AddModelError(string.Empty, "La solicitud contiene mediciones duplicadas.");
        }

        var preparado = await PrepararPaginaAsync(
            valoresPorConfiguracion,
            observacionesPorConfiguracion,
            permitirAmbientePredeterminado: false);

        if (!preparado || HorarioSeleccionado is null || AmbienteId is null)
        {
            ModelState.AddModelError(string.Empty, "El ambiente o el horario ya no está disponible.");
            return Page();
        }

        var idsEsperados = Mediciones.Select(x => x.AmbienteMedicionId).ToHashSet();
        var idsEnviados = medicionesEnviadas.Select(x => x.AmbienteMedicionId).ToHashSet();
        if (!idsEsperados.SetEquals(idsEnviados))
        {
            ModelState.AddModelError(string.Empty, "Las mediciones enviadas no corresponden al ambiente seleccionado.");
        }

        for (var indice = 0; indice < Mediciones.Count; indice++)
        {
            var medicion = Mediciones[indice];
            if (medicion.Valor is null)
            {
                ModelState.AddModelError($"Mediciones[{indice}].Valor", "Ingresa un valor.");
                continue;
            }

            if (decimal.Round(medicion.Valor.Value, medicion.DecimalesPermitidos) != medicion.Valor.Value)
            {
                ModelState.AddModelError(
                    $"Mediciones[{indice}].Valor",
                    $"Usa como máximo {medicion.DecimalesPermitidos} decimal(es).");
            }

            medicion.Observacion = string.IsNullOrWhiteSpace(medicion.Observacion)
                ? null
                : medicion.Observacion.Trim();
            if (medicion.Observacion?.Length > 500)
            {
                ModelState.AddModelError(
                    $"Mediciones[{indice}].Observacion",
                    "La observación admite hasta 500 caracteres.");
            }
        }

        if (Mediciones.Any(x =>
                x.Valor.HasValue &&
                EvaluarRango(x.Valor.Value, x.RangoMinimo, x.RangoMaximo) !=
                    EstadoRango.DentroDeRango) &&
            !ConfirmacionFueraDeRango)
        {
            ModelState.AddModelError(
                string.Empty,
                "Hay mediciones fuera del rango permitido. Revisa los valores y confirma expresamente el registro.");
        }

        MotivoFueraDePlazo = string.IsNullOrWhiteSpace(MotivoFueraDePlazo)
            ? null
            : MotivoFueraDePlazo.Trim();
        if (HorarioSeleccionado.Puntualidad == EstadoPuntualidad.FueraDePlazo)
        {
            if (MotivoFueraDePlazo is null)
            {
                ModelState.AddModelError(
                    nameof(MotivoFueraDePlazo),
                    "Explica por qué la medición se está regularizando fuera de plazo.");
            }
            else if (MotivoFueraDePlazo.Length > 500)
            {
                ModelState.AddModelError(
                    nameof(MotivoFueraDePlazo),
                    "El motivo admite hasta 500 caracteres.");
            }
        }
        else
        {
            MotivoFueraDePlazo = null;
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var usuarioId = _userManager.GetUserId(User)!;
        var ahoraLocal = _ventanaRegistroService.ObtenerAhoraLocal();
        var registro = new Registro
        {
            FechaOperativa = HorarioSeleccionado.FechaOperativa,
            AmbienteId = AmbienteId.Value,
            HorarioId = HorarioSeleccionado.HorarioId,
            UsuarioId = usuarioId,
            FechaHoraRegistro = ahoraLocal,
            Estado = EstadoRegistro.Confirmado,
            Puntualidad = HorarioSeleccionado.Puntualidad,
            MotivoFueraDePlazo = MotivoFueraDePlazo,
            Detalles = Mediciones.Select(x => new DetalleRegistro
            {
                AmbienteMedicionId = x.AmbienteMedicionId,
                TipoMedicionId = x.TipoMedicionId,
                Valor = x.Valor!.Value,
                LimiteMinimoAplicado = x.RangoMinimo,
                LimiteMaximoAplicado = x.RangoMaximo,
                EstadoRango = EvaluarRango(x.Valor.Value, x.RangoMinimo, x.RangoMaximo),
                Observacion = x.Observacion
            }).ToArray()
        };

        _context.Registros.Add(registro);

        if (HorarioSeleccionado.Puntualidad == EstadoPuntualidad.FueraDePlazo)
        {
            var incidencia = await _context.AlertasRegistrosOmitidos.SingleOrDefaultAsync(x =>
                x.FechaOperativa == HorarioSeleccionado.FechaOperativa &&
                x.AmbienteId == AmbienteId.Value &&
                x.HorarioId == HorarioSeleccionado.HorarioId);

            if (incidencia is null)
            {
                incidencia = new AlertaRegistroOmitido
                {
                    FechaOperativa = HorarioSeleccionado.FechaOperativa,
                    AmbienteId = AmbienteId.Value,
                    HorarioId = HorarioSeleccionado.HorarioId,
                    FechaHoraCierre = HorarioSeleccionado.Cierre,
                    FechaHoraDeteccion = ahoraLocal,
                    Estado = EstadoAlertaRegistroOmitido.Pendiente
                };
                _context.AlertasRegistrosOmitidos.Add(incidencia);
            }

            incidencia.EstadoIncidencia = EstadoIncidenciaRegistro.RegularizadaFueraDePlazo;
            incidencia.FechaHoraRegularizacion = ahoraLocal;
            incidencia.RegistroRegularizacion = registro;
            incidencia.Estado = EstadoAlertaRegistroOmitido.Pendiente;
            incidencia.UltimoError = null;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(
                exception,
                "No se pudo guardar el registro del ambiente {AmbienteId} y horario {HorarioId}.",
                AmbienteId,
                HorarioId);
            ModelState.AddModelError(
                string.Empty,
                "No se pudo guardar. Es posible que este horario ya haya sido registrado.");
            return Page();
        }

        MensajeExito = HorarioSeleccionado.Puntualidad == EstadoPuntualidad.FueraDePlazo
            ? $"Registro de {AmbienteSeleccionado} guardado fuera de plazo. La falta quedó pendiente de revisión."
            : $"Registro de {AmbienteSeleccionado} guardado correctamente.";
        return RedirectToPage(new { ambienteId = AmbienteId });
    }

    private async Task<bool> PrepararPaginaAsync(
        IReadOnlyDictionary<int, decimal?>? valoresEnviados,
        IReadOnlyDictionary<int, string?>? observacionesEnviadas,
        bool permitirAmbientePredeterminado)
    {
        var usuarioId = _userManager.GetUserId(User);
        if (usuarioId is null)
        {
            return false;
        }

        Ambientes = await ObtenerAmbientesAutorizadosAsync(usuarioId);
        if (Ambientes.Count == 0)
        {
            MensajeSinVentana = "No tienes ambientes asignados para registrar.";
            return true;
        }

        if (AmbienteId is null && permitirAmbientePredeterminado)
        {
            AmbienteId = Ambientes.FirstOrDefault(x => x.EsPredeterminado)?.Id ?? Ambientes[0].Id;
        }

        var ambiente = Ambientes.FirstOrDefault(x => x.Id == AmbienteId);
        if (ambiente is null)
        {
            return false;
        }

        AmbienteSeleccionado = ambiente.Nombre;
        var ahoraLocal = _ventanaRegistroService.ObtenerAhoraLocal();
        var fechaLocal = DateOnly.FromDateTime(ahoraLocal.DateTime);
        var fechaOperativaMinima = fechaLocal.AddDays(-3);
        var configuracionesHorario = await _context.AmbientesHorarios
            .AsNoTracking()
            .Include(x => x.Horario)
            .Where(x =>
                x.AmbienteId == ambiente.Id &&
                x.Horario.Activo &&
                x.VigenteDesde <= fechaLocal &&
                (x.VigenteHasta == null || x.VigenteHasta >= fechaOperativaMinima))
            .ToListAsync();

        var ventanas = _ventanaRegistroService.ObtenerVentanasAbiertas(configuracionesHorario, ahoraLocal);
        var fechasOperativas = ventanas.Select(x => x.FechaOperativa).Distinct().ToArray();
        var horariosRegistrados = fechasOperativas.Length == 0
            ? []
            : await _context.Registros
                .AsNoTracking()
                .Where(x => x.AmbienteId == ambiente.Id && fechasOperativas.Contains(x.FechaOperativa))
                .Select(x => new RegistroExistente(x.FechaOperativa, x.HorarioId))
                .ToListAsync();

        var existentes = horariosRegistrados
            .Select(x => (x.FechaOperativa, x.HorarioId))
            .ToHashSet();

        HorariosDisponibles = ventanas
            .Where(x => !existentes.Contains((x.FechaOperativa, x.Configuracion.HorarioId)))
            .OrderBy(x => x.Puntualidad == EstadoPuntualidad.FueraDePlazo ? 1 : 0)
            .ThenByDescending(x => x.HoraReferencia)
            .Select(x => new HorarioOpcion(
                x.Configuracion.HorarioId,
                x.Configuracion.Horario.Nombre,
                x.FechaOperativa,
                x.Apertura,
                x.LimitePuntualidad,
                x.Cierre,
                x.FinRegularizacion,
                x.Puntualidad))
            .ToArray();

        if (HorariosDisponibles.Count == 0)
        {
            MensajeSinVentana = ventanas.Count == 0
                ? "En este momento no hay un horario abierto para este ambiente."
                : "El registro del horario disponible ya fue completado.";
            return true;
        }

        if (HorarioId == 0 && !FechaOperativaSeleccionada.HasValue)
        {
            var rondaActual = HorariosDisponibles.FirstOrDefault(x =>
                x.Puntualidad != EstadoPuntualidad.FueraDePlazo);
            if (rondaActual is not null)
            {
                HorarioId = rondaActual.HorarioId;
                FechaOperativaSeleccionada = rondaActual.FechaOperativa;
            }
        }

        HorarioSeleccionado = FechaOperativaSeleccionada.HasValue
            ? HorariosDisponibles.FirstOrDefault(x =>
                x.HorarioId == HorarioId &&
                x.FechaOperativa == FechaOperativaSeleccionada.Value)
            : HorariosDisponibles.FirstOrDefault(x =>
                x.HorarioId == HorarioId &&
                x.Puntualidad != EstadoPuntualidad.FueraDePlazo);
        if (HorarioSeleccionado is null)
        {
            return true;
        }

        var fechaOperativa = HorarioSeleccionado.FechaOperativa;
        var configuracionesMedicionCandidatas = await _context.AmbientesMediciones
            .AsNoTracking()
            .Include(x => x.TipoMedicion)
            .Where(x =>
                x.AmbienteId == ambiente.Id &&
                x.TipoMedicion.Activo &&
                x.VigenteDesde <= fechaOperativa &&
                (x.VigenteHasta == null || x.VigenteHasta >= fechaOperativa))
            .OrderBy(x => x.TipoMedicionId)
            .ToListAsync();

        var configuracionesMedicion = configuracionesMedicionCandidatas
            .GroupBy(x => x.TipoMedicionId)
            .Select(x => x
                .OrderByDescending(y => y.Activo)
                .ThenByDescending(y => y.VigenteDesde)
                .ThenByDescending(y => y.Id)
                .First())
            .OrderBy(x => x.TipoMedicionId)
            .ToList();

        Mediciones = configuracionesMedicion.Select(x => new MedicionInput
        {
            AmbienteMedicionId = x.Id,
            TipoMedicionId = x.TipoMedicionId,
            Nombre = x.TipoMedicion.Nombre,
            Unidad = x.TipoMedicion.SimboloUnidad,
            DecimalesPermitidos = x.TipoMedicion.DecimalesPermitidos,
            RangoMinimo = x.RangoMinimo,
            RangoMaximo = x.RangoMaximo,
            Valor = valoresEnviados?.GetValueOrDefault(x.Id),
            Observacion = observacionesEnviadas?.GetValueOrDefault(x.Id)
        }).ToList();

        if (Mediciones.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "El ambiente no tiene mediciones configuradas para esta fecha.");
        }

        return true;
    }

    private async Task<IReadOnlyList<AmbienteOpcion>> ObtenerAmbientesAutorizadosAsync(string usuarioId)
    {
        if (User.IsInRole("Supervisor"))
        {
            return await _context.Ambientes
                .AsNoTracking()
                .Where(x => x.Activo)
                .OrderBy(x => x.Nombre)
                .Select(x => new AmbienteOpcion(x.Id, x.Nombre, false))
                .ToListAsync();
        }

        return await _context.UsuariosAmbientes
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.Activo && x.Ambiente.Activo)
            .OrderByDescending(x => x.EsPredeterminado)
            .ThenBy(x => x.Ambiente.Nombre)
            .Select(x => new AmbienteOpcion(x.AmbienteId, x.Ambiente.Nombre, x.EsPredeterminado))
            .ToListAsync();
    }

    private static EstadoRango EvaluarRango(decimal valor, decimal minimo, decimal maximo)
    {
        if (valor < minimo)
        {
            return EstadoRango.PorDebajo;
        }

        return valor > maximo ? EstadoRango.PorEncima : EstadoRango.DentroDeRango;
    }

    public sealed class MedicionInput
    {
        public int AmbienteMedicionId { get; set; }

        public int TipoMedicionId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Unidad { get; set; } = string.Empty;

        public byte DecimalesPermitidos { get; set; }

        public decimal RangoMinimo { get; set; }

        public decimal RangoMaximo { get; set; }

        [Required(ErrorMessage = "Ingresa un valor.")]
        public decimal? Valor { get; set; }

        public string? Observacion { get; set; }
    }

    public sealed record AmbienteOpcion(int Id, string Nombre, bool EsPredeterminado);

    public sealed record HorarioOpcion(
        int HorarioId,
        string Nombre,
        DateOnly FechaOperativa,
        DateTimeOffset Apertura,
        DateTimeOffset LimitePuntualidad,
        DateTimeOffset Cierre,
        DateTimeOffset FinRegularizacion,
        EstadoPuntualidad Puntualidad);

    private sealed record RegistroExistente(DateOnly FechaOperativa, int HorarioId);
}
