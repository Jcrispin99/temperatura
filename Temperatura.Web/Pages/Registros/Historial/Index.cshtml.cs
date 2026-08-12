using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Data;
using Temperatura.Web.Domain;
using Temperatura.Web.Domain.Enums;
using Temperatura.Web.Services;

namespace Temperatura.Web.Pages.Registros.Historial;

[Authorize]
public class IndexModel(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IVentanaRegistroService ventanaRegistroService) : PageModel
{
    private const int TamanoPagina = 25;
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IVentanaRegistroService _ventanaRegistroService = ventanaRegistroService;

    [BindProperty(SupportsGet = true)]
    public DateOnly? FechaDesde { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? FechaHasta { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? AmbienteId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? UsuarioId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? HorarioId { get; set; }

    [BindProperty(SupportsGet = true)]
    public EstadoPuntualidad? Puntualidad { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? FueraDeRango { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Pagina { get; set; } = 1;

    public IReadOnlyList<RegistroFila> Registros { get; private set; } = [];
    public IReadOnlyList<AmbienteOpcion> Ambientes { get; private set; } = [];
    public IReadOnlyList<UsuarioOpcion> Usuarios { get; private set; } = [];
    public IReadOnlyList<HorarioOpcion> Horarios { get; private set; } = [];
    public int TotalRegistros { get; private set; }
    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(TotalRegistros / (double)TamanoPagina));
    public bool EsSupervisor => User.IsInRole("Supervisor");

    public async Task OnGetAsync()
    {
        if (!Request.Query.ContainsKey(nameof(FechaDesde)) &&
            !Request.Query.ContainsKey(nameof(FechaHasta)))
        {
            var hoy = DateOnly.FromDateTime(_ventanaRegistroService.ObtenerAhoraLocal().DateTime);
            FechaDesde = hoy;
            FechaHasta = hoy;
        }

        Pagina = Math.Max(1, Pagina);
        var usuarioActualId = _userManager.GetUserId(User)!;
        var ambientesAutorizados = await ObtenerAmbientesAutorizadosAsync(usuarioActualId);
        var idsAmbientes = ambientesAutorizados.Select(x => x.Id).ToArray();

        Ambientes = ambientesAutorizados;
        Horarios = await _context.Horarios
            .AsNoTracking()
            .OrderBy(x => x.EsCierreDiaOperativoAnterior)
            .ThenBy(x => x.HoraReferencia)
            .Select(x => new HorarioOpcion(x.Id, x.Nombre))
            .ToListAsync();

        var usuarios = idsAmbientes.Length == 0
            ? []
            : await _context.Registros
                .AsNoTracking()
                .Where(x => idsAmbientes.Contains(x.AmbienteId))
                .Select(x => new { Id = x.UsuarioId, x.Usuario.Nombre, Email = x.Usuario.Email ?? string.Empty })
                .Distinct()
                .OrderBy(x => x.Nombre)
                .ThenBy(x => x.Email)
                .ToListAsync();
        Usuarios = usuarios
            .Select(x => new UsuarioOpcion(x.Id, x.Nombre, x.Email))
            .ToList();

        if (AmbienteId.HasValue && !idsAmbientes.Contains(AmbienteId.Value))
        {
            ModelState.AddModelError(string.Empty, "El ambiente seleccionado no está disponible para tu usuario.");
        }

        if (FechaDesde > FechaHasta)
        {
            ModelState.AddModelError(string.Empty, "La fecha inicial no puede ser posterior a la fecha final.");
        }

        if (!ModelState.IsValid || idsAmbientes.Length == 0)
        {
            return;
        }

        var consulta = _context.Registros
            .AsNoTracking()
            .Where(x => idsAmbientes.Contains(x.AmbienteId));

        if (FechaDesde.HasValue)
        {
            consulta = consulta.Where(x => x.FechaOperativa >= FechaDesde.Value);
        }

        if (FechaHasta.HasValue)
        {
            consulta = consulta.Where(x => x.FechaOperativa <= FechaHasta.Value);
        }

        if (AmbienteId.HasValue)
        {
            consulta = consulta.Where(x => x.AmbienteId == AmbienteId.Value);
        }

        if (!string.IsNullOrWhiteSpace(UsuarioId))
        {
            consulta = consulta.Where(x => x.UsuarioId == UsuarioId);
        }

        if (HorarioId.HasValue)
        {
            consulta = consulta.Where(x => x.HorarioId == HorarioId.Value);
        }

        if (Puntualidad.HasValue)
        {
            consulta = consulta.Where(x => x.Puntualidad == Puntualidad.Value);
        }

        if (FueraDeRango.HasValue)
        {
            consulta = FueraDeRango.Value
                ? consulta.Where(x => x.Detalles.Any(y => y.EstadoRango != EstadoRango.DentroDeRango))
                : consulta.Where(x => x.Detalles.All(y => y.EstadoRango == EstadoRango.DentroDeRango));
        }

        TotalRegistros = await consulta.CountAsync();
        if (Pagina > TotalPaginas)
        {
            Pagina = TotalPaginas;
        }

        Registros = await consulta
            .OrderByDescending(x => x.FechaOperativa)
            .ThenByDescending(x => x.Horario.HoraReferencia)
            .ThenByDescending(x => x.FechaHoraRegistro)
            .Skip((Pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .Select(x => new RegistroFila(
                x.Id,
                x.FechaOperativa,
                x.Ambiente.Nombre,
                x.Horario.Nombre,
                x.Usuario.Nombre,
                x.Usuario.Email ?? string.Empty,
                x.FechaHoraRegistro,
                x.Puntualidad,
                x.Detalles.Count,
                x.Detalles.Any(y => y.EstadoRango != EstadoRango.DentroDeRango)))
            .ToListAsync();
    }

    private async Task<IReadOnlyList<AmbienteOpcion>> ObtenerAmbientesAutorizadosAsync(string usuarioId)
    {
        if (EsSupervisor)
        {
            return await _context.Ambientes
                .AsNoTracking()
                .OrderByDescending(x => x.Activo)
                .ThenBy(x => x.Nombre)
                .Select(x => new AmbienteOpcion(x.Id, x.Nombre, x.Activo))
                .ToListAsync();
        }

        return await _context.UsuariosAmbientes
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.Activo)
            .OrderBy(x => x.Ambiente.Nombre)
            .Select(x => new AmbienteOpcion(x.AmbienteId, x.Ambiente.Nombre, x.Ambiente.Activo))
            .ToListAsync();
    }

    public sealed record RegistroFila(
        long Id,
        DateOnly FechaOperativa,
        string Ambiente,
        string Horario,
        string Usuario,
        string Email,
        DateTimeOffset FechaHoraRegistro,
        EstadoPuntualidad Puntualidad,
        int CantidadMediciones,
        bool FueraDeRango);

    public sealed record AmbienteOpcion(int Id, string Nombre, bool Activo);
    public sealed record UsuarioOpcion(string Id, string Nombre, string Email);
    public sealed record HorarioOpcion(int Id, string Nombre);
}
