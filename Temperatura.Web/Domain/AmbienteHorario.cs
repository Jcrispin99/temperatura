namespace Temperatura.Web.Domain;

public class AmbienteHorario
{
    public const short MinutosAntesPredeterminados = 30;

    public const short MinutosToleranciaPuntualidadPredeterminados = 30;

    public const short MinutosDespuesPredeterminados = 60;

    public const short MinutosRegularizacionPredeterminados = 720;

    public int Id { get; set; }

    public int AmbienteId { get; set; }

    public int HorarioId { get; set; }

    public short MinutosAntes { get; set; } = MinutosAntesPredeterminados;

    public short MinutosToleranciaPuntualidad { get; set; } =
        MinutosToleranciaPuntualidadPredeterminados;

    public short MinutosDespues { get; set; } = MinutosDespuesPredeterminados;

    public short MinutosRegularizacion { get; set; } = MinutosRegularizacionPredeterminados;

    public DateOnly VigenteDesde { get; set; }

    public DateOnly? VigenteHasta { get; set; }

    public bool Activo { get; set; } = true;

    public Ambiente Ambiente { get; set; } = null!;

    public Horario Horario { get; set; } = null!;
}
