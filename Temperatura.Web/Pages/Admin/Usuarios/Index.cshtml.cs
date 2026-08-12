using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Data;
using Temperatura.Web.Domain;

namespace Temperatura.Web.Pages.Admin.Usuarios;

[Authorize(Roles = "Supervisor")]
public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IReadOnlyList<UsuarioFila> Usuarios { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var usuarios = await _context.Users
            .AsNoTracking()
            .OrderBy(x => x.Nombre)
            .ThenBy(x => x.Email)
            .ToListAsync();

        var roles = await (
                from usuarioRol in _context.UserRoles.AsNoTracking()
                join rol in _context.Roles.AsNoTracking() on usuarioRol.RoleId equals rol.Id
                select new { usuarioRol.UserId, Rol = rol.Name! })
            .ToListAsync();

        var asignaciones = await _context.UsuariosAmbientes
            .AsNoTracking()
            .Where(x => x.Activo)
            .Include(x => x.Ambiente)
            .ToListAsync();

        Usuarios = usuarios.Select(usuario => new UsuarioFila(
            usuario.Id,
            usuario.Nombre,
            usuario.Email ?? string.Empty,
            usuario.Activo,
            roles.Where(x => x.UserId == usuario.Id).Select(x => x.Rol).FirstOrDefault() ?? "Sin rol",
            asignaciones
                .Where(x => x.UsuarioId == usuario.Id)
                .OrderByDescending(x => x.EsPredeterminado)
                .ThenBy(x => x.Ambiente.Nombre)
                .Select(x => x.Ambiente.Nombre + (x.EsPredeterminado ? " (predeterminado)" : string.Empty))
                .ToArray()))
            .ToArray();
    }

    public sealed record UsuarioFila(
        string Id,
        string Nombre,
        string Email,
        bool Activo,
        string Rol,
        IReadOnlyList<string> Ambientes);
}
