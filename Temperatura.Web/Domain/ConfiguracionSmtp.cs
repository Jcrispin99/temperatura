namespace Temperatura.Web.Domain;

public class ConfiguracionSmtp
{
    public int Id { get; set; }

    public bool Activo { get; set; } = true;

    public string Servidor { get; set; } = "smtp.gmail.com";

    public int Puerto { get; set; } = 587;

    public bool UsarTls { get; set; } = true;

    public string CorreoRemitente { get; set; } = string.Empty;

    public string NombreRemitente { get; set; } = string.Empty;

    public string Usuario { get; set; } = string.Empty;

    public string SecretoProtegido { get; set; } = string.Empty;

    public DateTimeOffset FechaActualizacion { get; set; }

    public string ActualizadoPorUsuarioId { get; set; } = string.Empty;
}
