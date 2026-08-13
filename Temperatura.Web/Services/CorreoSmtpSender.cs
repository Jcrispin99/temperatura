using System.Net;
using System.Net.Mail;
using Temperatura.Web.Domain;

namespace Temperatura.Web.Services;

public sealed class CorreoSmtpSender(IProtectorSecretoSmtp protectorSecreto) : ICorreoSmtpSender
{
    private readonly IProtectorSecretoSmtp _protectorSecreto = protectorSecreto;

    public async Task EnviarAsync(
        ConfiguracionSmtp configuracion,
        IReadOnlyCollection<string> destinatarios,
        string asunto,
        string cuerpoHtml,
        CancellationToken cancellationToken = default)
    {
        if (destinatarios.Count == 0)
        {
            throw new InvalidOperationException("No hay destinatarios para el correo.");
        }

        using var mensaje = new MailMessage
        {
            From = new MailAddress(configuracion.CorreoRemitente, configuracion.NombreRemitente),
            Subject = asunto,
            Body = cuerpoHtml,
            IsBodyHtml = true
        };

        foreach (var destinatario in destinatarios.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            mensaje.To.Add(destinatario);
        }

        using var cliente = new SmtpClient(configuracion.Servidor, configuracion.Puerto)
        {
            EnableSsl = configuracion.UsarTls,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(
                configuracion.Usuario,
                _protectorSecreto.Desproteger(configuracion.SecretoProtegido))
        };

        await cliente.SendMailAsync(mensaje, cancellationToken);
    }
}
