namespace Temperatura.Web.Domain;

public class UsuarioAmbiente
{
    public string UsuarioId { get; set; } = string.Empty;

    public int AmbienteId { get; set; }

    public bool EsPredeterminado { get; set; }

    public bool Activo { get; set; } = true;

    public ApplicationUser Usuario { get; set; } = null!;

    public Ambiente Ambiente { get; set; } = null!;
}
