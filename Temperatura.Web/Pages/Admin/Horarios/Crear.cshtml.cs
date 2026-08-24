using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Data;
using Temperatura.Web.Domain;
using Temperatura.Web.Services;

namespace Temperatura.Web.Pages.Admin.Horarios;

[Authorize(Roles = "Supervisor")]
public class CrearModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public HorarioInput Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var hora = Input.HoraReferencia!.Value;
        Input.Nombre = string.IsNullOrWhiteSpace(Input.Nombre)
            ? ValidadorHorarios.NombrePredeterminado(hora)
            : Input.Nombre.Trim();

        var existentes = await ObtenerValidablesAsync();
        var candidato = new HorarioValidable(
            0,
            hora,
            Input.EsCierreDiaOperativoAnterior,
            Input.Activo);

        foreach (var error in ValidadorHorarios.Validar(candidato, existentes))
        {
            ModelState.AddModelError(error.Clave, error.Mensaje);
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var horario = new Horario
        {
            Nombre = Input.Nombre,
            HoraReferencia = hora,
            EsCierreDiaOperativoAnterior = Input.EsCierreDiaOperativoAnterior,
            Activo = Input.Activo
        };
        _context.Horarios.Add(horario);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                ValidadorHorarios.ClaveHora,
                "Otro usuario acaba de registrar un horario a esa misma hora. Vuelve a intentarlo.");
            return Page();
        }

        TempData["MensajeExito"] =
            $"El horario {horario.Nombre} fue creado. Habilítalo en cada ambiente que deba cumplirlo.";
        return RedirectToPage("Index");
    }

    private async Task<List<HorarioValidable>> ObtenerValidablesAsync()
    {
        return await _context.Horarios
            .AsNoTracking()
            .Select(x => new HorarioValidable(
                x.Id,
                x.HoraReferencia,
                x.EsCierreDiaOperativoAnterior,
                x.Activo))
            .ToListAsync();
    }

    public sealed class HorarioInput
    {
        [StringLength(50, ErrorMessage = "El nombre admite hasta 50 caracteres.")]
        public string? Nombre { get; set; }

        [Required(ErrorMessage = "Ingresa la hora de referencia.")]
        [DataType(DataType.Time)]
        public TimeOnly? HoraReferencia { get; set; }

        public bool EsCierreDiaOperativoAnterior { get; set; }

        public bool Activo { get; set; } = true;
    }
}
