using Temperatura.Web.Domain;

namespace Temperatura.Web.Services;

public interface ICorreoSmtpSender
{
    Task EnviarAsync(
        ConfiguracionSmtp configuracion,
        IReadOnlyCollection<string> destinatarios,
        string asunto,
        string cuerpoHtml,
        CancellationToken cancellationToken = default);
}
