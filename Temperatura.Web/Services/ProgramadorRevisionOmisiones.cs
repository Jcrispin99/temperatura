using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Data;
using Temperatura.Web.Domain;

namespace Temperatura.Web.Services;

public sealed class ProgramadorRevisionOmisiones(
    ApplicationDbContext context,
    IVentanaRegistroService ventanaRegistroService,
    IConfiguration configuration) : IProgramadorRevisionOmisiones
{
    private readonly ApplicationDbContext _context = context;
    private readonly IVentanaRegistroService _ventanaRegistroService = ventanaRegistroService;
    private readonly TimeZoneInfo _zonaHoraria = TimeZoneInfo.FindSystemTimeZoneById(
        configuration["Sistema:ZonaHoraria"] ?? "America/Lima");

    public async Task<DateTimeOffset?> ObtenerProximoCierreAsync(
        CancellationToken cancellationToken = default)
    {
        var ahora = _ventanaRegistroService.ObtenerAhoraLocal();
        var hoy = DateOnly.FromDateTime(ahora.DateTime);
        var fechasOperativas = new[] { hoy.AddDays(-1), hoy, hoy.AddDays(1) };

        var configuraciones = await _context.AmbientesHorarios
            .AsNoTracking()
            .Include(x => x.Horario)
            .Where(x =>
                x.Ambiente.Activo &&
                x.Horario.Activo &&
                x.VigenteDesde <= fechasOperativas[2] &&
                (x.VigenteHasta == null || x.VigenteHasta >= fechasOperativas[0]))
            .ToListAsync(cancellationToken);

        var cierres = fechasOperativas.SelectMany(fecha => configuraciones
            .Where(x => x.VigenteDesde <= fecha &&
                        (x.VigenteHasta == null || x.VigenteHasta >= fecha))
            .GroupBy(x => new { x.AmbienteId, x.HorarioId })
            .Select(x => x
                .OrderByDescending(y => y.Activo)
                .ThenByDescending(y => y.VigenteDesde)
                .ThenByDescending(y => y.Id)
                .First())
            .Where(x => x.Activo)
            .Select(x => CrearCierre(fecha, x)));

        return CalculadorProximaRevision.SeleccionarProximoCierre(cierres, ahora);
    }

    private DateTimeOffset CrearCierre(DateOnly fechaOperativa, AmbienteHorario configuracion)
    {
        var fechaCalendario = configuracion.Horario.EsCierreDiaOperativoAnterior
            ? fechaOperativa.AddDays(1)
            : fechaOperativa;
        var fechaHora = DateTime.SpecifyKind(
            fechaCalendario.ToDateTime(configuracion.Horario.HoraReferencia),
            DateTimeKind.Unspecified);
        return new DateTimeOffset(fechaHora, _zonaHoraria.GetUtcOffset(fechaHora))
            .AddMinutes(configuracion.MinutosDespues);
    }
}
