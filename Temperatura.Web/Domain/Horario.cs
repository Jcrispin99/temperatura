namespace Temperatura.Web.Domain;

public class Horario
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public TimeOnly HoraReferencia { get; set; }

    public bool EsCierreDiaOperativoAnterior { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<AmbienteHorario> Ambientes { get; set; } = [];

    public ICollection<Registro> Registros { get; set; } = [];
}
