using Temperatura.Web.Domain.Enums;

namespace Temperatura.Web.Domain;

public class AlertaRegistroOmitido
{
    public long Id { get; set; }

    public DateOnly FechaOperativa { get; set; }

    public int AmbienteId { get; set; }

    public int HorarioId { get; set; }

    public DateTimeOffset FechaHoraCierre { get; set; }

    public DateTimeOffset FechaHoraDeteccion { get; set; }

    public EstadoAlertaRegistroOmitido Estado { get; set; } = EstadoAlertaRegistroOmitido.Pendiente;

    public EstadoIncidenciaRegistro EstadoIncidencia { get; set; } =
        EstadoIncidenciaRegistro.PendienteRegistro;

    public int IntentosEnvio { get; set; }

    public DateTimeOffset? FechaHoraEnvio { get; set; }

    public string? UltimoError { get; set; }

    public long? RegistroRegularizacionId { get; set; }

    public DateTimeOffset? FechaHoraRegularizacion { get; set; }

    public string? RevisadoPorUsuarioId { get; set; }

    public DateTimeOffset? FechaHoraRevision { get; set; }

    public string? ComentarioRevision { get; set; }

    public Ambiente Ambiente { get; set; } = null!;

    public Horario Horario { get; set; } = null!;

    public Registro? RegistroRegularizacion { get; set; }

    public ApplicationUser? RevisadoPorUsuario { get; set; }
}
