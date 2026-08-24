using Temperatura.Web.Domain.Enums;

namespace Temperatura.Web.Domain;

public class Registro
{
    public long Id { get; set; }

    public DateOnly FechaOperativa { get; set; }

    public int AmbienteId { get; set; }

    public int HorarioId { get; set; }

    public string UsuarioId { get; set; } = string.Empty;

    public DateTimeOffset FechaHoraRegistro { get; set; }

    public EstadoRegistro Estado { get; set; } = EstadoRegistro.Borrador;

    public EstadoPuntualidad Puntualidad { get; set; }

    public string? MotivoFueraDePlazo { get; set; }

    public Ambiente Ambiente { get; set; } = null!;

    public Horario Horario { get; set; } = null!;

    public ApplicationUser Usuario { get; set; } = null!;

    public ICollection<DetalleRegistro> Detalles { get; set; } = [];

    public AlertaRegistroOmitido? IncidenciaRegularizada { get; set; }
}
