using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Temperatura.Web.Domain;
using Temperatura.Web.Domain.Enums;
using Temperatura.Web.Services;

namespace Temperatura.Web.Pages.Registros;

[Authorize]
public class NuevoModel(
    UserManager<ApplicationUser> userManager,
    IRegistroCapturaService registroCapturaService) : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IRegistroCapturaService _registroCapturaService = registroCapturaService;

    [BindProperty(SupportsGet = true)]
    public int? AmbienteId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int HorarioId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? FechaOperativaSeleccionada { get; set; }

    [BindProperty]
    public List<MedicionCapturaInput> Mediciones { get; set; } = [];

    [BindProperty]
    public bool ConfirmacionFueraDeRango { get; set; }

    [BindProperty]
    public string? MotivoFueraDePlazo { get; set; }

    public IReadOnlyList<AmbienteCapturaOpcion> Ambientes { get; private set; } = [];

    public IReadOnlyList<HorarioCapturaOpcion> HorariosDisponibles { get; private set; } = [];

    public IReadOnlyList<HorarioCapturaOpcion> RondasPendientesRegularizacion =>
        HorariosDisponibles
            .Where(x => x.Puntualidad == EstadoPuntualidad.FueraDePlazo)
            .OrderByDescending(x => x.FechaOperativa)
            .ThenByDescending(x => x.Cierre)
            .ToArray();

    public IReadOnlyList<HorarioCapturaOpcion> RondasActualesDisponibles =>
        HorariosDisponibles
            .Where(x => x.Puntualidad != EstadoPuntualidad.FueraDePlazo)
            .OrderBy(x => x.Apertura)
            .ToArray();

    public HorarioCapturaOpcion? HorarioSeleccionado { get; private set; }

    public string? AmbienteSeleccionado { get; private set; }

    public string? MensajeSinVentana { get; private set; }

    [TempData]
    public string? MensajeExito { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var contexto = ObtenerContexto();
        if (contexto is null)
        {
            return Forbid();
        }

        var preparacion = await _registroCapturaService.PrepararAsync(
            contexto,
            new SeleccionCapturaRegistro(AmbienteId, HorarioId, FechaOperativaSeleccionada),
            permitirAmbientePredeterminado: true,
            cancellationToken: cancellationToken);
        Aplicar(preparacion);

        if (HorarioSeleccionado is not null && Mediciones.Count == 0)
        {
            ModelState.AddModelError(
                string.Empty,
                "El ambiente no tiene mediciones configuradas para esta fecha.");
        }

        return preparacion.Autorizado ? Page() : Forbid();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var contexto = ObtenerContexto();
        if (contexto is null)
        {
            return Forbid();
        }

        ModelState.Clear();
        var resultado = await _registroCapturaService.GuardarAsync(
            contexto,
            new SolicitudCapturaRegistro(
                AmbienteId,
                HorarioId,
                FechaOperativaSeleccionada,
                Mediciones,
                ConfirmacionFueraDeRango,
                MotivoFueraDePlazo),
            cancellationToken);

        Aplicar(resultado.Preparacion);
        MotivoFueraDePlazo = resultado.MotivoFueraDePlazo;
        foreach (var error in resultado.Errores)
        {
            ModelState.AddModelError(error.Clave, error.Mensaje);
        }

        if (!resultado.Guardado)
        {
            return Page();
        }

        MensajeExito = resultado.MensajeExito;
        return RedirectToPage(new { ambienteId = AmbienteId });
    }

    private ContextoCapturaRegistro? ObtenerContexto()
    {
        var usuarioId = _userManager.GetUserId(User);
        return usuarioId is null
            ? null
            : new ContextoCapturaRegistro(usuarioId, User.IsInRole("Supervisor"));
    }

    private void Aplicar(PreparacionCapturaRegistro preparacion)
    {
        AmbienteId = preparacion.AmbienteId;
        HorarioId = preparacion.HorarioId;
        FechaOperativaSeleccionada = preparacion.FechaOperativa;
        Ambientes = preparacion.Ambientes;
        HorariosDisponibles = preparacion.HorariosDisponibles;
        Mediciones = preparacion.Mediciones;
        HorarioSeleccionado = preparacion.HorarioSeleccionado;
        AmbienteSeleccionado = preparacion.AmbienteSeleccionado;
        MensajeSinVentana = preparacion.MensajeSinVentana;
    }
}
