using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Data;
using Temperatura.Web.Domain;
using Temperatura.Web.Services;

namespace Temperatura.Web.Pages;

public class IndexModel(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IAvanceDiarioService avanceDiarioService,
    IResumenAvanceService resumenAvanceService,
    IVentanaRegistroService ventanaRegistroService) : PageModel
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IAvanceDiarioService _avanceDiarioService = avanceDiarioService;
    private readonly IResumenAvanceService _resumenAvanceService = resumenAvanceService;
    private readonly IVentanaRegistroService _ventanaRegistroService = ventanaRegistroService;

    [BindProperty(SupportsGet = true)]
    public int? AmbienteId { get; set; }

    [BindProperty(SupportsGet = true)]
    public PeriodoDashboard Periodo { get; set; } = PeriodoDashboard.Diario;

    [BindProperty(SupportsGet = true)]
    public DateOnly? Fecha { get; set; }

    [BindProperty(SupportsGet = true)]
    public int[]? AmbienteIds { get; set; }

    public IReadOnlyList<AmbienteOpcion> Ambientes { get; private set; } = [];
    public IReadOnlyList<ResumenAvanceAmbiente> Resumenes { get; private set; } = [];
    public ResumenAvanceAmbiente? ResumenSeleccionado => Resumenes.FirstOrDefault();
    public ResumenAvancePeriodo? ResumenPeriodo { get; private set; }
    public string DatosGraficasAvanceJson { get; private set; } = "{}";
    public DateOnly FechaDesde { get; private set; }
    public DateOnly FechaHasta { get; private set; }
    public bool EsSupervisor => User.IsInRole("Supervisor");

    public async Task OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var usuarioId = _userManager.GetUserId(User)!;
        Ambientes = await ObtenerAmbientesAsync(usuarioId);
        if (Ambientes.Count == 0)
        {
            return;
        }

        if (EsSupervisor)
        {
            var hoy = DateOnly.FromDateTime(_ventanaRegistroService.ObtenerAhoraLocal().DateTime);
            Fecha ??= hoy;
            (FechaDesde, FechaHasta) = ObtenerRango(Periodo, Fecha.Value, hoy);

            var idsDisponibles = Ambientes.Select(x => x.Id).ToHashSet();
            var idsSeleccionados = AmbienteIds?
                .Where(idsDisponibles.Contains)
                .Distinct()
                .ToArray() ?? [];
            if (idsSeleccionados.Length == 0)
            {
                idsSeleccionados = idsDisponibles.ToArray();
            }

            AmbienteIds = idsSeleccionados;
            ResumenPeriodo = await _resumenAvanceService.ObtenerAsync(
                idsSeleccionados,
                FechaDesde,
                FechaHasta,
                HttpContext.RequestAborted);
            PrepararDatosGraficasAvance(ResumenPeriodo);
            return;
        }

        var ambienteSeleccionado = Ambientes.FirstOrDefault(x => x.Id == AmbienteId);
        if (ambienteSeleccionado is null)
        {
            ambienteSeleccionado = Ambientes.FirstOrDefault(x => x.EsPredeterminado) ?? Ambientes[0];
            AmbienteId = ambienteSeleccionado.Id;
        }

        Resumenes = await _avanceDiarioService.ObtenerAvanceActualAsync(
            [ambienteSeleccionado.Id],
            HttpContext.RequestAborted);
    }

    private async Task<IReadOnlyList<AmbienteOpcion>> ObtenerAmbientesAsync(string usuarioId)
    {
        if (EsSupervisor)
        {
            return await _context.Ambientes
                .AsNoTracking()
                .Where(x => x.Activo)
                .OrderBy(x => x.Nombre)
                .Select(x => new AmbienteOpcion(x.Id, x.Nombre, false))
                .ToListAsync();
        }

        return await _context.UsuariosAmbientes
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.Activo && x.Ambiente.Activo)
            .OrderByDescending(x => x.EsPredeterminado)
            .ThenBy(x => x.Ambiente.Nombre)
            .Select(x => new AmbienteOpcion(x.AmbienteId, x.Ambiente.Nombre, x.EsPredeterminado))
            .ToListAsync();
    }

    private static (DateOnly Desde, DateOnly Hasta) ObtenerRango(
        PeriodoDashboard periodo,
        DateOnly fechaReferencia,
        DateOnly hoy)
    {
        var desde = periodo switch
        {
            PeriodoDashboard.Semanal => fechaReferencia.AddDays(-(((int)fechaReferencia.DayOfWeek + 6) % 7)),
            PeriodoDashboard.Mensual => new DateOnly(fechaReferencia.Year, fechaReferencia.Month, 1),
            _ => fechaReferencia
        };
        var hasta = periodo switch
        {
            PeriodoDashboard.Semanal => desde.AddDays(6),
            PeriodoDashboard.Mensual => desde.AddMonths(1).AddDays(-1),
            _ => desde
        };

        return (desde, hasta > hoy ? hoy : hasta);
    }

    private void PrepararDatosGraficasAvance(ResumenAvancePeriodo resumen)
    {
        var datos = new DatosGraficasAvance(
            resumen.PorcentajeAvance,
            resumen.RegistrosEsperados,
            CrearDistribucion(
                resumen.RegistrosEsperados,
                resumen.RegistrosCompletados,
                resumen.RegistrosFueraDePlazo),
            resumen.Ambientes
                .OrderBy(x => x.PorcentajeAvance)
                .ThenBy(x => x.Ambiente)
                .Select(x => new AvanceAmbienteGrafica(
                    x.Ambiente,
                    x.RegistrosEsperados,
                    x.PorcentajeAvance,
                    CrearDistribucion(
                        x.RegistrosEsperados,
                        x.RegistrosCompletados,
                        x.RegistrosFueraDePlazo)))
                .ToList());

        DatosGraficasAvanceJson = JsonSerializer.Serialize(datos, OpcionesJson);
    }

    private static DistribucionAvance CrearDistribucion(
        int programados,
        int cumplidos,
        int fueraDePlazo)
    {
        var total = Math.Max(programados, 0);
        var cumplidosAplicables = Math.Min(Math.Max(cumplidos, 0), total);
        var noCumplidos = total - cumplidosAplicables;
        var regularizados = Math.Min(Math.Max(fueraDePlazo, 0), noCumplidos);

        return new DistribucionAvance(
            cumplidosAplicables,
            regularizados,
            noCumplidos - regularizados);
    }

    public sealed record AmbienteOpcion(int Id, string Nombre, bool EsPredeterminado);

    public sealed record DatosGraficasAvance(
        decimal PorcentajeAvance,
        int Programados,
        DistribucionAvance Distribucion,
        IReadOnlyList<AvanceAmbienteGrafica> Ambientes);

    public sealed record AvanceAmbienteGrafica(
        string Ambiente,
        int Programados,
        decimal PorcentajeAvance,
        DistribucionAvance Distribucion);

    public sealed record DistribucionAvance(
        int Cumplidos,
        int FueraDePlazo,
        int Pendientes);
}
