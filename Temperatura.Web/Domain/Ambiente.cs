namespace Temperatura.Web.Domain;

public class Ambiente
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public ICollection<UsuarioAmbiente> Usuarios { get; set; } = [];

    public ICollection<AmbienteMedicion> Mediciones { get; set; } = [];

    public ICollection<AmbienteHorario> Horarios { get; set; } = [];

    public ICollection<Registro> Registros { get; set; } = [];
}
