namespace Temperatura.Web.Domain;

public class TipoMedicion
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string SimboloUnidad { get; set; } = string.Empty;

    public byte DecimalesPermitidos { get; set; } = 1;

    public bool Activo { get; set; } = true;

    public ICollection<AmbienteMedicion> Ambientes { get; set; } = [];

    public ICollection<DetalleRegistro> Detalles { get; set; } = [];
}
