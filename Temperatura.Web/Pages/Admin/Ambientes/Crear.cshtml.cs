using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Data;
using Temperatura.Web.Domain;

namespace Temperatura.Web.Pages.Admin.Ambientes;

[Authorize(Roles = "Supervisor")]
public class CrearModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public AmbienteInput Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Input.Nombre = Input.Nombre.Trim();
        if (await _context.Ambientes.AnyAsync(x => x.Nombre == Input.Nombre))
        {
            ModelState.AddModelError("Input.Nombre", "Ya existe un ambiente con este nombre.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var ambiente = new Ambiente
        {
            Nombre = Input.Nombre,
            Activo = true
        };
        _context.Ambientes.Add(ambiente);
        await _context.SaveChangesAsync();

        TempData["MensajeExito"] = $"El ambiente {Input.Nombre} fue creado. Ahora configura sus mediciones y rangos.";
        return RedirectToPage("Editar", new { id = ambiente.Id });
    }

    public sealed class AmbienteInput
    {
        [Required(ErrorMessage = "Ingresa el nombre.")]
        [StringLength(100, ErrorMessage = "El nombre admite hasta 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;
    }
}
