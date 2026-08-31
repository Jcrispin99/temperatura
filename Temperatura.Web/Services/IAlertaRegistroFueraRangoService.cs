namespace Temperatura.Web.Services;

public interface IAlertaRegistroFueraRangoService
{
    Task<ResultadoAlertaFueraRango> RegistrarYNotificarAsync(
        long registroId,
        CancellationToken cancellationToken = default);

    Task<int> ReintentarPendientesAsync(CancellationToken cancellationToken = default);
}

public sealed record ResultadoAlertaFueraRango(
    bool TieneValoresFueraRango,
    bool CorreoEnviado);
