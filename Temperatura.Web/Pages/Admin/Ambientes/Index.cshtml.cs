using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Data;

namespace Temperatura.Web.Pages.Admin.Ambientes;

[Authorize(Roles = "Supervisor")]
public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IReadOnlyList<AmbienteFila> Ambientes { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Ambientes = await _context.Ambientes
            .AsNoTracking()
            .OrderByDescending(x => x.Activo)
            .ThenBy(x => x.Nombre)
            .Select(x => new AmbienteFila(
                x.Id,
                x.Nombre,
                x.Activo,
                x.Usuarios.Count(y => y.Activo),
                x.Mediciones.Count(y => y.Activo),
                x.Horarios.Count(y => y.Activo)))
            .ToListAsync();
    }

    public sealed record AmbienteFila(
        int Id,
        string Nombre,
        bool Activo,
        int Usuarios,
        int Mediciones,
        int Horarios);
}
