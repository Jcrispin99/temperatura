using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Data;
using Temperatura.Web.Domain;
using Temperatura.Web.Domain.Enums;
using Temperatura.Web.Services;

namespace Temperatura.Web.Pages.Admin.Incidencias;

[Authorize(Roles = "Supervisor")]
public class IndexModel(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IVentanaRegistroService ventanaRegistroService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IVentanaRegistroService _ventanaRegistroService = ventanaRegistroService;

    [BindProperty(SupportsGet = true)]
    public DateOnly? FechaDesde { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? FechaHasta { get; set; }

    [BindProperty(SupportsGet = true)]
    public EstadoIncidenciaRegistro? Estado { get; set; }

    public IReadOnlyList<IncidenciaFila> Incidencias { get; private set; } = [];

    [TempData]
    public string? MensajeExito { get; set; }

    public async Task OnGetAsync()
    {
        if (!Request.Query.ContainsKey(nameof(FechaDesde)) &&
            !Request.Query.ContainsKey(nameof(FechaHasta)))
        {
            var hoy = DateOnly.FromDateTime(_ventanaRegistroService.ObtenerAhoraLocal().DateTime);
            FechaDesde = hoy.AddDays(-7);
            FechaHasta = hoy;
        }

        if (FechaDesde > FechaHasta)
        {
            ModelState.AddModelError(string.Empty, "La fecha inicial no puede ser posterior a la fecha final.");
            return;
        }

        var consulta = _context.AlertasRegistrosOmitidos.AsNoTracking();
        if (FechaDesde.HasValue)
        {
            consulta = consulta.Where(x => x.FechaOperativa >= FechaDesde.Value);
        }

        if (FechaHasta.HasValue)
        {
            consulta = consulta.Where(x => x.FechaOperativa <= FechaHasta.Value);
        }

        if (Estado.HasValue)
        {
            consulta = consulta.Where(x => x.EstadoIncidencia == Estado.Value);
        }

        Incidencias = await consulta
            .OrderBy(x => x.EstadoIncidencia == EstadoIncidenciaRegistro.PendienteRegistro ? 0 :
                x.EstadoIncidencia == EstadoIncidenciaRegistro.RegularizadaFueraDePlazo ? 1 : 2)
            .ThenByDescending(x => x.FechaOperativa)
            .ThenBy(x => x.FechaHoraCierre)
            .Select(x => new IncidenciaFila(
                x.Id,
                x.FechaOperativa,
                x.Ambiente.Nombre,
                x.Horario.Nombre,
                x.FechaHoraCierre,
                x.EstadoIncidencia,
                x.RegistroRegularizacionId,
                x.FechaHoraRegularizacion,
                x.RegistroRegularizacion == null ? null : x.RegistroRegularizacion.Usuario.Nombre,
                x.RegistroRegularizacion == null ? null : x.RegistroRegularizacion.MotivoFueraDePlazo,
                x.FechaHoraRevision,
                x.RevisadoPorUsuario == null ? null : x.RevisadoPorUsuario.Nombre,
                x.ComentarioRevision))
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCambiarEstadoAsync(
        long id,
        EstadoIncidenciaRegistro nuevoEstado,
        string? comentario)
    {
        var estadosPermitidos = new[]
        {
            EstadoIncidenciaRegistro.Justificada,
            EstadoIncidenciaRegistro.FaltaConfirmada,
            EstadoIncidenciaRegistro.AmonestacionEmitida,
            EstadoIncidenciaRegistro.Descartada
        };
        if (!estadosPermitidos.Contains(nuevoEstado))
        {
            return BadRequest();
        }

        comentario = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim();
        if (comentario?.Length > 1000)
        {
            TempData["MensajeError"] = "El comentario de revisión admite hasta 1000 caracteres.";
            return RedirigirAlListado();
        }

        var incidencia = await _context.AlertasRegistrosOmitidos.SingleOrDefaultAsync(x => x.Id == id);
        if (incidencia is null)
        {
            return NotFound();
        }

        incidencia.EstadoIncidencia = nuevoEstado;
        incidencia.RevisadoPorUsuarioId = _userManager.GetUserId(User);
        incidencia.FechaHoraRevision = _ventanaRegistroService.ObtenerAhoraLocal();
        incidencia.ComentarioRevision = comentario;
        await _context.SaveChangesAsync();

        MensajeExito = $"La incidencia quedó como {ObtenerEtiqueta(nuevoEstado).ToLowerInvariant()}.";
        return RedirigirAlListado();
    }

    private IActionResult RedirigirAlListado() => RedirectToPage(new
    {
        FechaDesde,
        FechaHasta,
        Estado
    });

    public static string ObtenerEtiqueta(EstadoIncidenciaRegistro estado) => estado switch
    {
        EstadoIncidenciaRegistro.PendienteRegistro => "Sin registro",
        EstadoIncidenciaRegistro.RegularizadaFueraDePlazo => "Regularizada fuera de plazo",
        EstadoIncidenciaRegistro.Justificada => "Justificada",
        EstadoIncidenciaRegistro.FaltaConfirmada => "Falta confirmada",
        EstadoIncidenciaRegistro.AmonestacionEmitida => "Amonestación emitida",
        _ => "Descartada"
    };

    public sealed record IncidenciaFila(
        long Id,
        DateOnly FechaOperativa,
        string Ambiente,
        string Horario,
        DateTimeOffset FechaHoraCierre,
        EstadoIncidenciaRegistro Estado,
        long? RegistroId,
        DateTimeOffset? FechaHoraRegularizacion,
        string? RegistradoPor,
        string? Motivo,
        DateTimeOffset? FechaHoraRevision,
        string? RevisadoPor,
        string? ComentarioRevision);
}
