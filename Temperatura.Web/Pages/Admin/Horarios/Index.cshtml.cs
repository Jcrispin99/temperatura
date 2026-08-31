using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Data;
using Temperatura.Web.Domain.Enums;

namespace Temperatura.Web.Pages.Admin.Horarios;

[Authorize(Roles = "Supervisor")]
public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IReadOnlyList<HorarioFila> Horarios { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Horarios = await _context.Horarios
            .AsNoTracking()
            .OrderBy(x => x.EsCierreDiaOperativoAnterior)
            .ThenBy(x => x.HoraReferencia)
            .Select(x => new HorarioFila(
                x.Id,
                x.Nombre,
                x.HoraReferencia,
                x.MomentoOperativo,
                x.EsCierreDiaOperativoAnterior,
                x.Activo,
                x.Ambientes.Count(y => y.Activo),
                x.Registros.Count()))
            .ToListAsync();
    }

    public sealed record HorarioFila(
        int Id,
        string Nombre,
        TimeOnly HoraReferencia,
        MomentoOperativo MomentoOperativo,
        bool EsCierreDiaOperativoAnterior,
        bool Activo,
        int Ambientes,
        int Registros);
}
