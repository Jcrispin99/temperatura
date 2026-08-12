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
public class CrearModel(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager) : PageModel
{
    private static readonly string[] RolesPermitidos = ["Registrador", "Supervisor"];
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    [BindProperty]
    public UsuarioInput Input { get; set; } = new();

    public IReadOnlyList<AmbienteOpcion> Ambientes { get; private set; } = [];

    public async Task OnGetAsync()
    {
        await CargarAmbientesAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await CargarAmbientesAsync();
        ValidarAsignaciones();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var usuario = new ApplicationUser
        {
            Nombre = Input.Nombre.Trim(),
            Email = Input.Email.Trim(),
            UserName = Input.Email.Trim(),
            EmailConfirmed = true,
            Activo = true
        };

        var estrategia = _context.Database.CreateExecutionStrategy();
        try
        {
            await estrategia.ExecuteAsync(async () =>
            {
                await using var transaccion = await _context.Database.BeginTransactionAsync();
                var resultadoCreacion = await _userManager.CreateAsync(usuario, Input.Contrasena);
                VerificarResultado(resultadoCreacion);

                var resultadoRol = await _userManager.AddToRoleAsync(usuario, Input.Rol);
                VerificarResultado(resultadoRol);

                if (Input.Rol == "Registrador")
                {
                    _context.UsuariosAmbientes.AddRange(Input.AmbienteIds.Distinct().Select(ambienteId =>
                        new UsuarioAmbiente
                        {
                            UsuarioId = usuario.Id,
                            AmbienteId = ambienteId,
                            EsPredeterminado = ambienteId == Input.AmbientePredeterminadoId,
                            Activo = true
                        }));
                    await _context.SaveChangesAsync();
                }

                await transaccion.CommitAsync();
            });
        }
        catch (OperacionIdentityException exception)
        {
            AgregarErrores(exception.Resultado);
            return Page();
        }
        TempData["MensajeExito"] = $"El usuario {usuario.Nombre} fue creado correctamente.";
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

    private void ValidarAsignaciones()
    {
        if (!RolesPermitidos.Contains(Input.Rol))
        {
            ModelState.AddModelError("Input.Rol", "Selecciona un rol válido.");
            return;
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
        [Required(ErrorMessage = "Ingresa el nombre.")]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresa el correo.")]
        [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresa una contraseña.")]
        [DataType(DataType.Password)]
        public string Contrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirma la contraseña.")]
        [Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        public string ConfirmarContrasena { get; set; } = string.Empty;

        [Required]
        public string Rol { get; set; } = "Registrador";

        public List<int> AmbienteIds { get; set; } = [];

        public int? AmbientePredeterminadoId { get; set; }
    }

    public sealed record AmbienteOpcion(int Id, string Nombre);
}
