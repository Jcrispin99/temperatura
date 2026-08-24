using Temperatura.Web.Domain;
using Temperatura.Web.Domain.Enums;

namespace Temperatura.Web.Services;

public sealed class VentanaRegistroService : IVentanaRegistroService
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _zonaHoraria;

    public VentanaRegistroService(TimeProvider timeProvider, IConfiguration configuration)
    {
        _timeProvider = timeProvider;
        var identificadorZona = configuration["Sistema:ZonaHoraria"] ?? "America/Lima";
        _zonaHoraria = TimeZoneInfo.FindSystemTimeZoneById(identificadorZona);
    }

    public DateTimeOffset ObtenerAhoraLocal()
    {
        return TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), _zonaHoraria);
    }

    public IReadOnlyList<VentanaRegistroAbierta> ObtenerVentanasAbiertas(
        IEnumerable<AmbienteHorario> configuraciones,
        DateTimeOffset ahoraLocal)
    {
        var configuracionesDisponibles = configuraciones
            .Where(x => x.Horario.Activo)
            .ToList();
        var fechaLocal = DateOnly.FromDateTime(ahoraLocal.DateTime);
        var minutosRegularizacionMaximos = configuracionesDisponibles.Count == 0
            ? 0
            : configuracionesDisponibles.Max(x => x.MinutosRegularizacion);
        var diasHaciaAtras = (int)Math.Ceiling(minutosRegularizacionMaximos / 1440d) + 1;
        var fechasOperativasPosibles = Enumerable.Range(0, diasHaciaAtras + 1)
            .Select(indice => fechaLocal.AddDays(-indice))
            .ToArray();
        var ventanas = new List<VentanaRegistroAbierta>();

        foreach (var grupoHorario in configuracionesDisponibles.GroupBy(x => x.HorarioId))
        {
            foreach (var fechaOperativa in fechasOperativasPosibles)
            {
                var configuracion = grupoHorario
                    .Where(x =>
                        x.VigenteDesde <= fechaOperativa &&
                        (x.VigenteHasta is null || x.VigenteHasta >= fechaOperativa))
                    .OrderByDescending(x => x.Activo)
                    .ThenByDescending(x => x.VigenteDesde)
                    .ThenByDescending(x => x.Id)
                    .FirstOrDefault();

                if (configuracion is null)
                {
                    continue;
                }

                var fechaCalendario = configuracion.Horario.EsCierreDiaOperativoAnterior
                    ? fechaOperativa.AddDays(1)
                    : fechaOperativa;

                var horaReferencia = CrearInstanteLocal(
                    fechaCalendario,
                    configuracion.Horario.HoraReferencia);
                var apertura = horaReferencia.AddMinutes(-configuracion.MinutosAntes);
                var limitePuntualidad = horaReferencia.AddMinutes(
                    configuracion.MinutosToleranciaPuntualidad);
                var cierre = horaReferencia.AddMinutes(configuracion.MinutosDespues);
                var finRegularizacion = cierre.AddMinutes(configuracion.MinutosRegularizacion);

                if (ahoraLocal < apertura || ahoraLocal >= finRegularizacion)
                {
                    continue;
                }

                var puntualidad = ahoraLocal >= cierre
                    ? EstadoPuntualidad.FueraDePlazo
                    : ahoraLocal <= limitePuntualidad
                        ? EstadoPuntualidad.Puntual
                        : EstadoPuntualidad.Tardio;

                ventanas.Add(new VentanaRegistroAbierta(
                    configuracion,
                    fechaOperativa,
                    apertura,
                    horaReferencia,
                    limitePuntualidad,
                    cierre,
                    finRegularizacion,
                    puntualidad));
            }
        }

        return ventanas
            .OrderBy(x => x.HoraReferencia)
            .ToArray();
    }

    private DateTimeOffset CrearInstanteLocal(DateOnly fecha, TimeOnly hora)
    {
        var fechaHora = DateTime.SpecifyKind(fecha.ToDateTime(hora), DateTimeKind.Unspecified);
        return new DateTimeOffset(fechaHora, _zonaHoraria.GetUtcOffset(fechaHora));
    }
}
