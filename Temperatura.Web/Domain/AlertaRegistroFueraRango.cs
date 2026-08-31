using Temperatura.Web.Domain.Enums;

namespace Temperatura.Web.Domain;

public class AlertaRegistroFueraRango
{
    public long Id { get; set; }

    public long RegistroId { get; set; }

    public DateTimeOffset FechaHoraDeteccion { get; set; }

    public EstadoAlertaRango Estado { get; set; } = EstadoAlertaRango.Pendiente;

    public int IntentosEnvio { get; set; }

    public DateTimeOffset? FechaHoraEnvio { get; set; }

    public string? UltimoError { get; set; }

    public Registro Registro { get; set; } = null!;
}
