namespace Temperatura.Web.Domain;

public class AmbienteHorario
{
    public int Id { get; set; }

    public int AmbienteId { get; set; }

    public int HorarioId { get; set; }

    public short MinutosAntes { get; set; } = 30;

    public short MinutosDespues { get; set; } = 60;

    public DateOnly VigenteDesde { get; set; }

    public DateOnly? VigenteHasta { get; set; }

    public bool Activo { get; set; } = true;

    public Ambiente Ambiente { get; set; } = null!;

    public Horario Horario { get; set; } = null!;
}
