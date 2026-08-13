namespace Temperatura.Web.Services;

public sealed class AlertaRegistroOmitidoWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<AlertaRegistroOmitidoWorker> logger) : BackgroundService
{
    private static readonly TimeSpan ToleranciaPosterior = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ReintentoSinHorarios = TimeSpan.FromHours(6);
    private static readonly TimeSpan ReintentoTrasError = TimeSpan.FromMinutes(15);
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<AlertaRegistroOmitidoWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RevisarAsync(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            DateTimeOffset? proximoCierre;
            try
            {
                proximoCierre = await ObtenerProximoCierreAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "No se pudo calcular el próximo cierre; se reintentará en {Minutos} minutos.",
                    ReintentoTrasError.TotalMinutes);
                await Task.Delay(ReintentoTrasError, _timeProvider, stoppingToken);
                continue;
            }

            var espera = proximoCierre.HasValue
                ? proximoCierre.Value + ToleranciaPosterior - _timeProvider.GetUtcNow()
                : ReintentoSinHorarios;

            if (espera > TimeSpan.Zero)
            {
                if (proximoCierre.HasValue)
                {
                    _logger.LogInformation(
                        "La próxima revisión de registros omitidos se ejecutará después del cierre {ProximoCierre}.",
                        proximoCierre.Value);
                }

                await Task.Delay(espera, _timeProvider, stoppingToken);
            }

            await RevisarAsync(stoppingToken);
        }
    }

    private async Task<DateTimeOffset?> ObtenerProximoCierreAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var programador = scope.ServiceProvider.GetRequiredService<IProgramadorRevisionOmisiones>();
        return await programador.ObtenerProximoCierreAsync(cancellationToken);
    }

    private async Task RevisarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var servicio = scope.ServiceProvider.GetRequiredService<IAlertaRegistroOmitidoService>();
            await servicio.RevisarYNotificarAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Falló la revisión automática de registros omitidos.");
        }
    }
}
