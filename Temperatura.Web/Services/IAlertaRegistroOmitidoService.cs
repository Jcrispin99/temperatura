namespace Temperatura.Web.Services;

public interface IAlertaRegistroOmitidoService
{
    Task<ResultadoRevisionOmisiones> RevisarYNotificarAsync(
        CancellationToken cancellationToken = default);
}

public sealed record ResultadoRevisionOmisiones(
    int OmisionesDetectadas,
    int AlertasEnviadas,
    bool CorreoEnviado);
