namespace Temperatura.Web.Domain;

public class AmbienteMedicion
{
    public int Id { get; set; }

    public int AmbienteId { get; set; }

    public int TipoMedicionId { get; set; }

    public decimal RangoMinimo { get; set; }

    public decimal RangoMaximo { get; set; }

    public DateOnly VigenteDesde { get; set; }

    public DateOnly? VigenteHasta { get; set; }

    public bool Activo { get; set; } = true;

    public Ambiente Ambiente { get; set; } = null!;

    public TipoMedicion TipoMedicion { get; set; } = null!;

    public ICollection<DetalleRegistro> Detalles { get; set; } = [];
}
