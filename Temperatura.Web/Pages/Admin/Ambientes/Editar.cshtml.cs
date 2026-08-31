using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Temperatura.Web.Services;

namespace Temperatura.Web.Pages.Admin.Ambientes;

[Authorize(Roles = "Supervisor")]
public class EditarModel(IConfiguracionAmbienteService configuracionAmbienteService) : PageModel
{
    private readonly IConfiguracionAmbienteService _configuracionAmbienteService =
        configuracionAmbienteService;

    [BindProperty]
    public AmbienteConfiguracionInput Input { get; set; } = new();

    public IReadOnlyList<HorarioAmbienteHistorico> HistorialHorarios { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var resultado = await _configuracionAmbienteService.ObtenerAsync(id, cancellationToken);
        if (!resultado.Encontrado)
        {
            return NotFound();
        }

        Input = resultado.Input;
        HistorialHorarios = resultado.HistorialHorarios;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        var resultado = await _configuracionAmbienteService.ActualizarAsync(
            Input,
            cancellationToken);
        if (!resultado.Encontrado)
        {
            return NotFound();
        }

        Input = resultado.Input;
        HistorialHorarios = resultado.HistorialHorarios;
        foreach (var error in resultado.Errores)
        {
            ModelState.AddModelError(error.Clave, error.Mensaje);
        }

        if (!resultado.Guardado)
        {
            return Page();
        }

        TempData["MensajeExito"] =
            $"El ambiente {resultado.NombreAmbiente}, sus mediciones y horarios fueron actualizados.";
        return RedirectToPage("Index");
    }
}
