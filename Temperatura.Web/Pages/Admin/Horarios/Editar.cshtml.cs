using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Data;
using Temperatura.Web.Domain.Enums;
using Temperatura.Web.Services;

namespace Temperatura.Web.Pages.Admin.Horarios;

[Authorize(Roles = "Supervisor")]
public class EditarModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public HorarioInput Input { get; set; } = new();

    public int RegistrosHistoricos { get; private set; }

    public int AmbientesQueLoUsan { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var horario = await _context.Horarios
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == Id);
        if (horario is null)
        {
            return NotFound();
        }

        Input = new HorarioInput
        {
            Nombre = horario.Nombre,
            HoraReferencia = horario.HoraReferencia,
            MomentoOperativo = horario.MomentoOperativo,
            EsCierreDiaOperativoAnterior = horario.EsCierreDiaOperativoAnterior,
            Activo = horario.Activo
        };
        await CargarContextoAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var horario = await _context.Horarios.SingleOrDefaultAsync(x => x.Id == Id);
        if (horario is null)
        {
            return NotFound();
        }

        await CargarContextoAsync();
        ValidarMomentoOperativo();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var hora = Input.HoraReferencia!.Value;
        Input.Nombre = string.IsNullOrWhiteSpace(Input.Nombre)
            ? ValidadorHorarios.NombrePredeterminado(hora)
            : Input.Nombre.Trim();

        var existentes = await _context.Horarios
            .AsNoTracking()
            .Select(x => new HorarioValidable(
                x.Id,
                x.HoraReferencia,
                x.EsCierreDiaOperativoAnterior,
                x.Activo))
            .ToListAsync();
        var candidato = new HorarioValidable(
            Id,
            hora,
            Input.EsCierreDiaOperativoAnterior,
            Input.Activo);

        foreach (var error in ValidadorHorarios.Validar(candidato, existentes))
        {
            ModelState.AddModelError(error.Clave, error.Mensaje);
        }

        await ValidarAsignacionesDelMomentoAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        horario.Nombre = Input.Nombre;
        horario.HoraReferencia = hora;
        horario.MomentoOperativo = Input.MomentoOperativo!.Value;
        horario.EsCierreDiaOperativoAnterior = Input.EsCierreDiaOperativoAnterior;
        horario.Activo = Input.Activo;

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

        TempData["MensajeExito"] = $"El horario {horario.Nombre} fue actualizado.";
        return RedirectToPage("Index");
    }

    private void ValidarMomentoOperativo()
    {
        if (!Input.MomentoOperativo.HasValue)
        {
            return;
        }

        var esMedianoche = Input.MomentoOperativo == MomentoOperativo.Medianoche;
        if (esMedianoche != Input.EsCierreDiaOperativoAnterior)
        {
            ModelState.AddModelError(
                "Input.MomentoOperativo",
                "Medianoche debe cerrar el día operativo anterior; los demás momentos pertenecen al mismo día.");
        }
    }

    private async Task ValidarAsignacionesDelMomentoAsync()
    {
        if (!Input.MomentoOperativo.HasValue)
        {
            return;
        }

        var ambientesAsignados = _context.AmbientesHorarios
            .Where(x => x.HorarioId == Id && x.Activo)
            .Select(x => x.AmbienteId);
        var ambientesEnConflicto = await _context.AmbientesHorarios
            .AsNoTracking()
            .Where(x =>
                x.Activo &&
                x.HorarioId != Id &&
                x.Horario.MomentoOperativo == Input.MomentoOperativo.Value &&
                ambientesAsignados.Contains(x.AmbienteId))
            .Select(x => x.Ambiente.Nombre)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        if (ambientesEnConflicto.Count > 0)
        {
            ModelState.AddModelError(
                "Input.MomentoOperativo",
                $"El momento ya tiene otro horario asignado en: {string.Join(", ", ambientesEnConflicto)}.");
        }
    }

    private async Task CargarContextoAsync()
    {
        RegistrosHistoricos = await _context.Registros.CountAsync(x => x.HorarioId == Id);
        AmbientesQueLoUsan = await _context.AmbientesHorarios
            .CountAsync(x => x.HorarioId == Id && x.Activo);
    }

    public sealed class HorarioInput
    {
        [StringLength(50, ErrorMessage = "El nombre admite hasta 50 caracteres.")]
        public string? Nombre { get; set; }

        [Required(ErrorMessage = "Ingresa la hora de referencia.")]
        [DataType(DataType.Time)]
        public TimeOnly? HoraReferencia { get; set; }

        [Required(ErrorMessage = "Selecciona el momento operativo.")]
        public MomentoOperativo? MomentoOperativo { get; set; }

        public bool EsCierreDiaOperativoAnterior { get; set; }

        public bool Activo { get; set; } = true;
    }
}
