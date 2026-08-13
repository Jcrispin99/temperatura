using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Data;
using Temperatura.Web.Domain;
using Temperatura.Web.Services;

namespace Temperatura.Web.Pages.Admin.Smtp;

[Authorize(Roles = "Supervisor")]
public class IndexModel(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IProtectorSecretoSmtp protectorSecreto,
    ICorreoSmtpSender correoSender,
    TimeProvider timeProvider) : PageModel
{
    private const int ConfiguracionId = 1;
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IProtectorSecretoSmtp _protectorSecreto = protectorSecreto;
    private readonly ICorreoSmtpSender _correoSender = correoSender;
    private readonly TimeProvider _timeProvider = timeProvider;

    [BindProperty]
    public ConfiguracionInput Input { get; set; } = new();

    public bool TieneSecretoConfigurado { get; private set; }

    public DateTimeOffset? FechaActualizacion { get; private set; }

    public async Task OnGetAsync()
    {
        var configuracion = await _context.ConfiguracionesSmtp
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == ConfiguracionId);

        if (configuracion is null)
        {
            return;
        }

        Input = new ConfiguracionInput
        {
            Activo = configuracion.Activo,
            Servidor = configuracion.Servidor,
            Puerto = configuracion.Puerto,
            UsarTls = configuracion.UsarTls,
            CorreoRemitente = configuracion.CorreoRemitente,
            NombreRemitente = configuracion.NombreRemitente,
            Usuario = configuracion.Usuario
        };
        TieneSecretoConfigurado = !string.IsNullOrWhiteSpace(configuracion.SecretoProtegido);
        FechaActualizacion = configuracion.FechaActualizacion;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var configuracion = await _context.ConfiguracionesSmtp
            .SingleOrDefaultAsync(x => x.Id == ConfiguracionId);
        TieneSecretoConfigurado = configuracion is not null &&
                                  !string.IsNullOrWhiteSpace(configuracion.SecretoProtegido);
        FechaActualizacion = configuracion?.FechaActualizacion;

        if (!TieneSecretoConfigurado && string.IsNullOrWhiteSpace(Input.Secreto))
        {
            ModelState.AddModelError(
                "Input.Secreto",
                "Ingresa la contraseña de aplicación de Google.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        configuracion ??= new ConfiguracionSmtp();
        configuracion.Activo = Input.Activo;
        configuracion.Servidor = Input.Servidor.Trim();
        configuracion.Puerto = Input.Puerto;
        configuracion.UsarTls = Input.UsarTls;
        configuracion.CorreoRemitente = Input.CorreoRemitente.Trim();
        configuracion.NombreRemitente = Input.NombreRemitente.Trim();
        configuracion.Usuario = Input.Usuario.Trim();
        configuracion.FechaActualizacion = _timeProvider.GetUtcNow();
        configuracion.ActualizadoPorUsuarioId = _userManager.GetUserId(User)!;

        if (!string.IsNullOrWhiteSpace(Input.Secreto))
        {
            var secretoNormalizado = string.Concat(Input.Secreto.Where(x => !char.IsWhiteSpace(x)));
            configuracion.SecretoProtegido = _protectorSecreto.Proteger(secretoNormalizado);
        }

        if (_context.Entry(configuracion).State == EntityState.Detached)
        {
            _context.ConfiguracionesSmtp.Add(configuracion);
        }

        await _context.SaveChangesAsync();
        TempData["MensajeExito"] = "La configuración SMTP fue guardada correctamente.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostProbarAsync(CancellationToken cancellationToken)
    {
        var configuracion = await _context.ConfiguracionesSmtp
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == ConfiguracionId, cancellationToken);
        var usuario = await _userManager.GetUserAsync(User);

        if (configuracion is null || string.IsNullOrWhiteSpace(configuracion.SecretoProtegido))
        {
            TempData["MensajeError"] = "Guarda primero una configuración SMTP completa.";
            return RedirectToPage();
        }

        if (usuario?.Email is null)
        {
            TempData["MensajeError"] = "El supervisor actual no tiene un correo configurado.";
            return RedirectToPage();
        }

        try
        {
            await _correoSender.EnviarAsync(
                configuracion,
                [usuario.Email],
                "Prueba de correo - Control ambiental",
                "<h2>Configuración SMTP correcta</h2><p>Este correo confirma que el sistema puede enviar alertas mediante Gmail.</p>",
                cancellationToken);
            TempData["MensajeExito"] = $"Correo de prueba enviado a {usuario.Email}.";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            TempData["MensajeError"] = $"No se pudo enviar el correo de prueba: {exception.Message}";
        }

        return RedirectToPage();
    }

    public sealed class ConfiguracionInput
    {
        public bool Activo { get; set; } = true;

        [Required(ErrorMessage = "Ingresa el servidor SMTP.")]
        [StringLength(255)]
        public string Servidor { get; set; } = "smtp.gmail.com";

        [Range(1, 65535, ErrorMessage = "Ingresa un puerto válido.")]
        public int Puerto { get; set; } = 587;

        public bool UsarTls { get; set; } = true;

        [Required(ErrorMessage = "Ingresa el correo remitente.")]
        [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
        public string CorreoRemitente { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresa el nombre del remitente.")]
        [StringLength(150)]
        public string NombreRemitente { get; set; } = "Control ambiental";

        [Required(ErrorMessage = "Ingresa el usuario SMTP.")]
        [EmailAddress(ErrorMessage = "Para Gmail, el usuario SMTP debe ser un correo válido.")]
        public string Usuario { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string? Secreto { get; set; }
    }
}
