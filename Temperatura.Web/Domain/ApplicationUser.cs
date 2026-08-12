using Microsoft.AspNetCore.Identity;

namespace Temperatura.Web.Domain;

public class ApplicationUser : IdentityUser
{
    public string Nombre { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public ICollection<UsuarioAmbiente> Ambientes { get; set; } = [];

    public ICollection<Registro> Registros { get; set; } = [];
}
