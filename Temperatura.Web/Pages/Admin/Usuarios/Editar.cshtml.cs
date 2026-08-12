using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Data;
using Temperatura.Web.Domain;

namespace Temperatura.Web.Pages.Admin.Usuarios;

[Authorize(Roles = "Supervisor")]
public class EditarModel(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager) : PageModel
{
    private static readonly string[] RolesPermitidos = ["Registrador", "Supervisor"];
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    [BindProperty]
    public UsuarioInput Input { get; set; } = new();

    public IReadOnlyList<AmbienteOpcion> Ambientes { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var usuario = await _userManager.FindByIdAsync(id);
        if (usuario is null)
        {
            return NotFound();
        }

        await CargarAmbientesAsync();
        var roles = await _userManager.GetRolesAsync(usuario);
        var asignaciones = await _context.UsuariosAmbientes
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuario.Id && x.Activo)
            .ToListAsync();

        Input = new UsuarioInput
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Email = usuario.Email ?? string.Empty,
            Rol = roles.FirstOrDefault() ?? "Registrador",
            Activo = usuario.Activo,
            AmbienteIds = asignaciones.Select(x => x.AmbienteId).ToList(),
            AmbientePredeterminadoId = asignaciones.FirstOrDefault(x => x.EsPredeterminado)?.AmbienteId
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await CargarAmbientesAsync();
        var usuario = await _userManager.FindByIdAsync(Input.Id);
        if (usuario is null)
        {
            return NotFound();
        }

        await ValidarCambiosAsync(usuario);
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var estrategia = _context.Database.CreateExecutionStrategy();
        try
        {
            await estrategia.ExecuteAsync(async () =>
            {
                await using var transaccion = await _context.Database.BeginTransactionAsync();

                usuario.Nombre = Input.Nombre.Trim();
                usuario.Email = Input.Email.Trim();
                usuario.UserName = Input.Email.Trim();
                usuario.Activo = Input.Activo;

                var resultadoActualizacion = await _userManager.UpdateAsync(usuario);
                VerificarResultado(resultadoActualizacion);

                var rolesActuales = await _userManager.GetRolesAsync(usuario);
                if (rolesActuales.Count > 0 && !rolesActuales.Contains(Input.Rol))
                {
                    var resultadoRemoverRol = await _userManager.RemoveFromRolesAsync(usuario, rolesActuales);
                    VerificarResultado(resultadoRemoverRol);
                }

                if (!await _userManager.IsInRoleAsync(usuario, Input.Rol))
                {
                    var resultadoAgregarRol = await _userManager.AddToRoleAsync(usuario, Input.Rol);
                    VerificarResultado(resultadoAgregarRol);
                }

                var asignacionesActuales = await _context.UsuariosAmbientes
                    .Where(x => x.UsuarioId == usuario.Id)
                    .ToListAsync();

                if (Input.Rol == "Registrador")
                {
                    var ambientesSeleccionados = Input.AmbienteIds.Distinct().ToHashSet();
                    foreach (var asignacion in asignacionesActuales)
                    {
                        asignacion.Activo = ambientesSeleccionados.Contains(asignacion.AmbienteId);
                        asignacion.EsPredeterminado =
                            asignacion.Activo && asignacion.AmbienteId == Input.AmbientePredeterminadoId;
                    }

                    var ambientesExistentes = asignacionesActuales.Select(x => x.AmbienteId).ToHashSet();
                    _context.UsuariosAmbientes.AddRange(ambientesSeleccionados
                        .Where(ambienteId => !ambientesExistentes.Contains(ambienteId))
                        .Select(ambienteId => new UsuarioAmbiente
                        {
                            UsuarioId = usuario.Id,
                            AmbienteId = ambienteId,
                            EsPredeterminado = ambienteId == Input.AmbientePredeterminadoId,
                            Activo = true
                        }));
                }
                else
                {
                    foreach (var asignacion in asignacionesActuales)
                    {
                        asignacion.Activo = false;
                        asignacion.EsPredeterminado = false;
                    }
                }

                await _context.SaveChangesAsync();

                if (!string.IsNullOrWhiteSpace(Input.NuevaContrasena))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
                    var resultadoContrasena = await _userManager.ResetPasswordAsync(
                        usuario,
                        token,
                        Input.NuevaContrasena);
                    VerificarResultado(resultadoContrasena);
                }

                if (!usuario.Activo)
                {
                    var resultadoSello = await _userManager.UpdateSecurityStampAsync(usuario);
                    VerificarResultado(resultadoSello);
                }

                await transaccion.CommitAsync();
            });
        }
        catch (OperacionIdentityException exception)
        {
            AgregarErrores(exception.Resultado);
            return Page();
        }
        TempData["MensajeExito"] = $"El usuario {usuario.Nombre} fue actualizado correctamente.";
        return RedirectToPage("Index");
    }

    private async Task CargarAmbientesAsync()
    {
        Ambientes = await _context.Ambientes
            .AsNoTracking()
            .Where(x => x.Activo)
            .OrderBy(x => x.Nombre)
            .Select(x => new AmbienteOpcion(x.Id, x.Nombre))
            .ToListAsync();
    }

    private async Task ValidarCambiosAsync(ApplicationUser usuario)
    {
        if (!RolesPermitidos.Contains(Input.Rol))
        {
            ModelState.AddModelError("Input.Rol", "Selecciona un rol válido.");
            return;
        }

        var usuarioActualId = _userManager.GetUserId(User);
        if (usuario.Id == usuarioActualId && !Input.Activo)
        {
            ModelState.AddModelError("Input.Activo", "No puedes desactivar tu propia cuenta.");
        }

        if (usuario.Id == usuarioActualId && Input.Rol != "Supervisor")
        {
            ModelState.AddModelError("Input.Rol", "No puedes quitarte el rol Supervisor.");
        }

        var esSupervisorActualmente = await _userManager.IsInRoleAsync(usuario, "Supervisor");
        if (esSupervisorActualmente && (!Input.Activo || Input.Rol != "Supervisor"))
        {
            var supervisoresActivos = await (
                    from usuarioRol in _context.UserRoles
                    join rol in _context.Roles on usuarioRol.RoleId equals rol.Id
                    join usuarioActivo in _context.Users on usuarioRol.UserId equals usuarioActivo.Id
                    where rol.NormalizedName == "SUPERVISOR" && usuarioActivo.Activo
                    select usuarioActivo.Id)
                .CountAsync();

            if (supervisoresActivos <= 1)
            {
                ModelState.AddModelError(string.Empty, "Debe existir al menos un supervisor activo.");
            }
        }

        if (Input.Rol != "Registrador")
        {
            return;
        }

        var ambientesActivos = Ambientes.Select(x => x.Id).ToHashSet();
        var ambientesSeleccionados = Input.AmbienteIds.Distinct().ToHashSet();
        if (ambientesSeleccionados.Count == 0 || !ambientesSeleccionados.IsSubsetOf(ambientesActivos))
        {
            ModelState.AddModelError("Input.AmbienteIds", "Selecciona al menos un ambiente activo.");
        }

        if (Input.AmbientePredeterminadoId is null ||
            !ambientesSeleccionados.Contains(Input.AmbientePredeterminadoId.Value))
        {
            ModelState.AddModelError(
                "Input.AmbientePredeterminadoId",
                "Selecciona como predeterminado uno de los ambientes asignados.");
        }
    }

    private void AgregarErrores(IdentityResult resultado)
    {
        foreach (var error in resultado.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private static void VerificarResultado(IdentityResult resultado)
    {
        if (!resultado.Succeeded)
        {
            throw new OperacionIdentityException(resultado);
        }
    }

    private sealed class OperacionIdentityException(IdentityResult resultado) : Exception
    {
        public IdentityResult Resultado { get; } = resultado;
    }

    public sealed class UsuarioInput
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresa el nombre.")]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresa el correo.")]
        [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Rol { get; set; } = "Registrador";

        public bool Activo { get; set; } = true;

        public List<int> AmbienteIds { get; set; } = [];

        public int? AmbientePredeterminadoId { get; set; }

        [DataType(DataType.Password)]
        public string? NuevaContrasena { get; set; }

        [Compare(nameof(NuevaContrasena), ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        public string? ConfirmarContrasena { get; set; }
    }

    public sealed record AmbienteOpcion(int Id, string Nombre);
}
