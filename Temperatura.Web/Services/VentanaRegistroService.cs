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
        var fechaLocal = DateOnly.FromDateTime(ahoraLocal.DateTime);
        var fechasOperativasPosibles = new[] { fechaLocal, fechaLocal.AddDays(-1) };
        var ventanas = new List<VentanaRegistroAbierta>();

        foreach (var configuracion in configuraciones.Where(x => x.Activo && x.Horario.Activo))
        {
            foreach (var fechaOperativa in fechasOperativasPosibles)
            {
                if (configuracion.VigenteDesde > fechaOperativa ||
                    configuracion.VigenteHasta < fechaOperativa)
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
                var cierre = horaReferencia.AddMinutes(configuracion.MinutosDespues);

                if (ahoraLocal < apertura || ahoraLocal >= cierre)
                {
                    continue;
                }

                var puntualidad = ahoraLocal <= horaReferencia
                    ? EstadoPuntualidad.Puntual
                    : EstadoPuntualidad.Tardio;

                ventanas.Add(new VentanaRegistroAbierta(
                    configuracion,
                    fechaOperativa,
                    apertura,
                    horaReferencia,
                    cierre,
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
