using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Data;
using Temperatura.Web.Domain;
using Temperatura.Web.Domain.Enums;

namespace Temperatura.Web.Services;

public interface IResumenAvanceService
{
    Task<ResumenAvancePeriodo> ObtenerAsync(
        IReadOnlyCollection<int> ambienteIds,
        DateOnly fechaDesde,
        DateOnly fechaHasta,
        CancellationToken cancellationToken = default);
}

public enum PeriodoDashboard
{
    Diario = 1,
    Semanal = 2,
    Mensual = 3
}

public sealed class ResumenAvanceService(ApplicationDbContext context) : IResumenAvanceService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<ResumenAvancePeriodo> ObtenerAsync(
        IReadOnlyCollection<int> ambienteIds,
        DateOnly fechaDesde,
        DateOnly fechaHasta,
        CancellationToken cancellationToken = default)
    {
        if (fechaDesde > fechaHasta)
        {
            throw new ArgumentException("La fecha inicial no puede ser posterior a la fecha final.");
        }

        var ids = ambienteIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new ResumenAvancePeriodo(0, 0, 0, 0, 0, []);
        }

        var ambientes = await _context.Ambientes
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .OrderBy(x => x.Nombre)
            .Select(x => new AmbienteResumen(x.Id, x.Nombre, 0, 0, 0))
            .ToListAsync(cancellationToken);

        var configuraciones = await _context.AmbientesHorarios
            .AsNoTracking()
            .Where(x =>
                ids.Contains(x.AmbienteId) &&
                x.VigenteDesde <= fechaHasta &&
                (x.VigenteHasta == null || x.VigenteHasta >= fechaDesde))
            .ToListAsync(cancellationToken);

        var esperados = new HashSet<(int AmbienteId, DateOnly FechaOperativa, int HorarioId)>();
        foreach (var ambiente in ambientes)
        {
            for (var fecha = fechaDesde; fecha <= fechaHasta; fecha = fecha.AddDays(1))
            {
                foreach (var configuracion in configuraciones
                    .Where(x =>
                        x.AmbienteId == ambiente.AmbienteId &&
                        x.VigenteDesde <= fecha &&
                        (x.VigenteHasta == null || x.VigenteHasta >= fecha))
                    .GroupBy(x => x.HorarioId)
                    .Select(x => x
                        .OrderByDescending(y => y.Activo)
                        .ThenByDescending(y => y.VigenteDesde)
                        .ThenByDescending(y => y.Id)
                        .First())
                    .Where(x => x.Activo))
                {
                    esperados.Add((ambiente.AmbienteId, fecha, configuracion.HorarioId));
                }
            }
        }

        var registros = await _context.Registros
            .AsNoTracking()
            .Where(x =>
                ids.Contains(x.AmbienteId) &&
                x.FechaOperativa >= fechaDesde &&
                x.FechaOperativa <= fechaHasta &&
                x.Estado == EstadoRegistro.Confirmado)
            .Select(x => new { x.AmbienteId, x.FechaOperativa, x.HorarioId })
            .ToListAsync(cancellationToken);

        var completadosPorAmbiente = registros
            .Where(x => esperados.Contains((x.AmbienteId, x.FechaOperativa, x.HorarioId)))
            .GroupBy(x => x.AmbienteId)
            .ToDictionary(x => x.Key, x => x.Count());
        var esperadosPorAmbiente = esperados
            .GroupBy(x => x.AmbienteId)
            .ToDictionary(x => x.Key, x => x.Count());

        var resumenes = ambientes
            .Select(ambiente =>
            {
                var esperadosAmbiente = esperadosPorAmbiente.GetValueOrDefault(ambiente.AmbienteId);
                var completadosAmbiente = completadosPorAmbiente.GetValueOrDefault(ambiente.AmbienteId);
                var noRegistrados = Math.Max(esperadosAmbiente - completadosAmbiente, 0);
                var porcentaje = CalcularPorcentaje(completadosAmbiente, esperadosAmbiente);

                return ambiente with
                {
                    RegistrosEsperados = esperadosAmbiente,
                    RegistrosCompletados = completadosAmbiente,
                    PorcentajeAvance = porcentaje
                };
            })
            .ToList();

        var esperadosTotal = resumenes.Sum(x => x.RegistrosEsperados);
        var completadosTotal = resumenes.Sum(x => x.RegistrosCompletados);
        return new ResumenAvancePeriodo(
            resumenes.Count,
            esperadosTotal,
            completadosTotal,
            Math.Max(esperadosTotal - completadosTotal, 0),
            CalcularPorcentaje(completadosTotal, esperadosTotal),
            resumenes);
    }

    private static decimal CalcularPorcentaje(int completados, int esperados) =>
        esperados == 0 ? 0 : Math.Round(completados * 100m / esperados, 2);
}

public sealed record ResumenAvancePeriodo(
    int AmbientesConsiderados,
    int RegistrosEsperados,
    int RegistrosCompletados,
    int RegistrosNoRegistrados,
    decimal PorcentajeAvance,
    IReadOnlyList<AmbienteResumen> Ambientes);

public sealed record AmbienteResumen(
    int AmbienteId,
    string Ambiente,
    int RegistrosEsperados,
    int RegistrosCompletados,
    decimal PorcentajeAvance);