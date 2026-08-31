using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Data;
using Temperatura.Web.Domain;
using Temperatura.Web.Domain.Enums;

namespace Temperatura.Web.Pages.Registros.Historial;

[Authorize]
public class DetalleModel(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public RegistroDetalle Registro { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(long id)
    {
        var usuarioId = _userManager.GetUserId(User)!;
        var consulta = _context.Registros.AsNoTracking().Where(x => x.Id == id);

        if (!User.IsInRole("Supervisor"))
        {
            consulta = consulta.Where(x => x.Ambiente.Usuarios.Any(y =>
                y.UsuarioId == usuarioId && y.Activo));
        }

        var registro = await consulta
            .Select(x => new RegistroDetalle(
                x.Id,
                x.FechaOperativa,
                x.Ambiente.Nombre,
                x.HorarioNombreAplicado,
                x.Usuario.Nombre,
                x.Usuario.Email ?? string.Empty,
                x.FechaHoraRegistro,
                x.Puntualidad,
                x.MotivoFueraDePlazo,
                x.Detalles
                    .OrderBy(y => y.TipoMedicionId)
                    .Select(y => new MedicionDetalle(
                        y.TipoMedicion.Nombre,
                        y.TipoMedicion.SimboloUnidad,
                        y.Valor,
                        y.LimiteMinimoAplicado,
                        y.LimiteMaximoAplicado,
                        y.EstadoRango,
                        y.Observacion))
                    .ToList()))
            .SingleOrDefaultAsync();

        if (registro is null)
        {
            return NotFound();
        }

        Registro = registro;
        return Page();
    }

    public sealed record RegistroDetalle(
        long Id,
        DateOnly FechaOperativa,
        string Ambiente,
        string Horario,
        string Usuario,
        string Email,
        DateTimeOffset FechaHoraRegistro,
        EstadoPuntualidad Puntualidad,
        string? MotivoFueraDePlazo,
        IReadOnlyList<MedicionDetalle> Mediciones);

    public sealed record MedicionDetalle(
        string Nombre,
        string Unidad,
        decimal Valor,
        decimal LimiteMinimo,
        decimal LimiteMaximo,
        EstadoRango EstadoRango,
        string? Observacion);
}
