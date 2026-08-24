using Temperatura.Web.Domain;
using Temperatura.Web.Domain.Enums;

namespace Temperatura.Web.Services;

public interface IVentanaRegistroService
{
    DateTimeOffset ObtenerAhoraLocal();

    IReadOnlyList<VentanaRegistroAbierta> ObtenerVentanasAbiertas(
        IEnumerable<AmbienteHorario> configuraciones,
        DateTimeOffset ahoraLocal);
}

public sealed record VentanaRegistroAbierta(
    AmbienteHorario Configuracion,
    DateOnly FechaOperativa,
    DateTimeOffset Apertura,
    DateTimeOffset HoraReferencia,
    DateTimeOffset LimitePuntualidad,
    DateTimeOffset Cierre,
    DateTimeOffset FinRegularizacion,
    EstadoPuntualidad Puntualidad);
