using Temperatura.Web.Domain.Enums;

namespace Temperatura.Web.Domain;

public class DetalleRegistro
{
    public long Id { get; set; }

    public long RegistroId { get; set; }

    public int AmbienteMedicionId { get; set; }

    public int TipoMedicionId { get; set; }

    public decimal Valor { get; set; }

    public decimal LimiteMinimoAplicado { get; set; }

    public decimal LimiteMaximoAplicado { get; set; }

    public EstadoRango EstadoRango { get; set; }

    public Registro Registro { get; set; } = null!;

    public AmbienteMedicion AmbienteMedicion { get; set; } = null!;

    public TipoMedicion TipoMedicion { get; set; } = null!;
}
