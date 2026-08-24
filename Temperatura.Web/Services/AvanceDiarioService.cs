using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Data;
using Temperatura.Web.Domain;
using Temperatura.Web.Domain.Enums;

namespace Temperatura.Web.Services;

public sealed class AvanceDiarioService(
    ApplicationDbContext context,
    IVentanaRegistroService ventanaRegistroService,
    IConfiguration configuration) : IAvanceDiarioService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IVentanaRegistroService _ventanaRegistroService = ventanaRegistroService;
    private readonly TimeZoneInfo _zonaHoraria = TimeZoneInfo.FindSystemTimeZoneById(
        configuration["Sistema:ZonaHoraria"] ?? "America/Lima");

    public async Task<IReadOnlyList<ResumenAvanceAmbiente>> ObtenerAvanceActualAsync(
        IReadOnlyCollection<int> ambienteIds,
        CancellationToken cancellationToken = default)
    {
        if (ambienteIds.Count == 0)
        {
            return [];
        }

        var ids = ambienteIds.Distinct().ToArray();
        var ahora = _ventanaRegistroService.ObtenerAhoraLocal();
        var hoy = DateOnly.FromDateTime(ahora.DateTime);
        var ayer = hoy.AddDays(-1);

        var ambientes = await _context.Ambientes
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .OrderBy(x => x.Nombre)
            .Select(x => new { x.Id, x.Nombre })
            .ToListAsync(cancellationToken);

        var configuraciones = await _context.AmbientesHorarios
            .AsNoTracking()
            .Include(x => x.Horario)
            .Where(x =>
                ids.Contains(x.AmbienteId) &&
                x.Horario.Activo &&
                x.VigenteDesde <= hoy &&
                (x.VigenteHasta == null || x.VigenteHasta >= ayer))
            .ToListAsync(cancellationToken);

        var horaInicioPredeterminada = await ObtenerHoraInicioPredeterminadaAsync(cancellationToken);

        var fechasPorAmbiente = ambientes.ToDictionary(
            x => x.Id,
            x => DeterminarFechaOperativa(
                configuraciones.Where(y => y.AmbienteId == x.Id).ToList(),
                hoy,
                ahora,
                horaInicioPredeterminada));
        var fechas = fechasPorAmbiente.Values.Distinct().ToArray();

        var registros = await _context.Registros
            .AsNoTracking()
            .Include(x => x.Detalles)
                .ThenInclude(x => x.TipoMedicion)
            .Include(x => x.Horario)
            .Where(x =>
                ids.Contains(x.AmbienteId) &&
                fechas.Contains(x.FechaOperativa) &&
                x.Estado == EstadoRegistro.Confirmado)
            .ToListAsync(cancellationToken);

        var resultado = new List<ResumenAvanceAmbiente>(ambientes.Count);
        foreach (var ambiente in ambientes)
        {
            var fechaOperativa = fechasPorAmbiente[ambiente.Id];
            var horarios = SeleccionarConfiguracionesVigentes(
                configuraciones.Where(x => x.AmbienteId == ambiente.Id),
                fechaOperativa);
            var idsHorariosEsperados = horarios.Select(x => x.HorarioId).ToHashSet();
            var registrosAmbiente = registros
                .Where(x =>
                    x.AmbienteId == ambiente.Id &&
                    x.FechaOperativa == fechaOperativa &&
                    idsHorariosEsperados.Contains(x.HorarioId))
                .ToDictionary(x => x.HorarioId);

            var resumenHorarios = horarios.Select(configuracion =>
            {
                registrosAmbiente.TryGetValue(configuracion.HorarioId, out var registro);
                var fechaCalendario = configuracion.Horario.EsCierreDiaOperativoAnterior
                    ? fechaOperativa.AddDays(1)
                    : fechaOperativa;
                var referencia = CrearInstanteLocal(fechaCalendario, configuracion.Horario.HoraReferencia);
                var apertura = referencia.AddMinutes(-configuracion.MinutosAntes);
                var cierre = referencia.AddMinutes(configuracion.MinutosDespues);
                var fueraDeRango = registro?.Detalles.Any(x =>
                    x.EstadoRango != EstadoRango.DentroDeRango) ?? false;

                return new ResumenHorarioDiario(
                    configuracion.HorarioId,
                    configuracion.Horario.Nombre,
                    apertura,
                    referencia,
                    cierre,
                    AvanceDiarioCalculator.ObtenerEstado(
                        registro is not null && registro.Puntualidad != EstadoPuntualidad.FueraDePlazo,
                        ahora,
                        apertura,
                        cierre),
                    registro?.Id,
                    registro?.FechaHoraRegistro,
                    registro?.Puntualidad,
                    fueraDeRango);
            }).ToList();

            var completados = resumenHorarios.Count(x => x.Estado == EstadoHorarioDiario.Completado);
            var exigibles = resumenHorarios.Count(x => x.Apertura <= ahora);
            var cumplidosExigibles = resumenHorarios.Count(x =>
                x.Apertura <= ahora && x.Estado == EstadoHorarioDiario.Completado);
            var proxima = resumenHorarios
                .Where(x => x.Estado is EstadoHorarioDiario.Pendiente or EstadoHorarioDiario.Proximo)
                .OrderBy(x => x.Estado == EstadoHorarioDiario.Pendiente ? 0 : 1)
                .ThenBy(x => x.Apertura)
                .FirstOrDefault();

            var alertas = registrosAmbiente.Values
                .SelectMany(registro => registro.Detalles
                    .Where(x => x.EstadoRango != EstadoRango.DentroDeRango)
                    .Select(x => new AlertaRangoReciente(
                        registro.Id,
                        registro.Horario.Nombre,
                        x.TipoMedicion.Nombre,
                        x.TipoMedicion.SimboloUnidad,
                        x.Valor,
                        x.LimiteMinimoAplicado,
                        x.LimiteMaximoAplicado,
                        x.EstadoRango,
                        registro.FechaHoraRegistro)))
                .OrderByDescending(x => x.FechaHoraRegistro)
                .Take(5)
                .ToList();

            resultado.Add(new ResumenAvanceAmbiente(
                ambiente.Id,
                ambiente.Nombre,
                fechaOperativa,
                completados,
                resumenHorarios.Count,
                AvanceDiarioCalculator.CalcularPorcentaje(completados, resumenHorarios.Count),
                cumplidosExigibles,
                exigibles,
                exigibles == 0
                    ? null
                    : AvanceDiarioCalculator.CalcularPorcentaje(cumplidosExigibles, exigibles),
                registrosAmbiente.Values.Count(x => x.Puntualidad == EstadoPuntualidad.Tardio),
                registrosAmbiente.Values.Count(x => x.Puntualidad == EstadoPuntualidad.FueraDePlazo),
                registrosAmbiente.Values.Count(x => x.Detalles.Any(y =>
                    y.EstadoRango != EstadoRango.DentroDeRango)),
                resumenHorarios,
                proxima,
                alertas));
        }

        return resultado;
    }

    private async Task<TimeOnly?> ObtenerHoraInicioPredeterminadaAsync(
        CancellationToken cancellationToken)
    {
        return await _context.Horarios
            .AsNoTracking()
            .Where(x => x.Activo && !x.EsCierreDiaOperativoAnterior)
            .OrderBy(x => x.HoraReferencia)
            .Select(x => (TimeOnly?)x.HoraReferencia)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private DateOnly DeterminarFechaOperativa(
        IReadOnlyCollection<AmbienteHorario> configuraciones,
        DateOnly hoy,
        DateTimeOffset ahora,
        TimeOnly? horaInicioPredeterminada)
    {
        var vigentesHoy = SeleccionarConfiguracionesVigentes(configuraciones, hoy);
        var primeraApertura = vigentesHoy
            .Where(x => !x.Horario.EsCierreDiaOperativoAnterior)
            .Select(x => CrearInstanteLocal(hoy, x.Horario.HoraReferencia)
                .AddMinutes(-x.MinutosAntes))
            .OrderBy(x => x)
            .FirstOrDefault();

        if (primeraApertura == default && horaInicioPredeterminada.HasValue)
        {
            // El ambiente aún no tiene rondas asignadas: se usa la primera del catálogo.
            primeraApertura = CrearInstanteLocal(hoy, horaInicioPredeterminada.Value)
                .AddMinutes(-AmbienteHorario.MinutosAntesPredeterminados);
        }

        return AvanceDiarioCalculator.DeterminarFechaOperativa(ahora, primeraApertura);
    }

    private static IReadOnlyList<AmbienteHorario> SeleccionarConfiguracionesVigentes(
        IEnumerable<AmbienteHorario> configuraciones,
        DateOnly fechaOperativa)
    {
        return configuraciones
            .Where(x =>
                x.VigenteDesde <= fechaOperativa &&
                (x.VigenteHasta is null || x.VigenteHasta >= fechaOperativa))
            .GroupBy(x => x.HorarioId)
            .Select(x => x
                .OrderByDescending(y => y.Activo)
                .ThenByDescending(y => y.VigenteDesde)
                .ThenByDescending(y => y.Id)
                .First())
            .OrderBy(x => x.Horario.EsCierreDiaOperativoAnterior)
            .ThenBy(x => x.Horario.HoraReferencia)
            .ToList();
    }

    private DateTimeOffset CrearInstanteLocal(DateOnly fecha, TimeOnly hora)
    {
        var fechaHora = DateTime.SpecifyKind(fecha.ToDateTime(hora), DateTimeKind.Unspecified);
        return new DateTimeOffset(fechaHora, _zonaHoraria.GetUtcOffset(fechaHora));
    }
}
