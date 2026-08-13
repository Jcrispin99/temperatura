namespace Temperatura.Web.Services;

public interface IProgramadorRevisionOmisiones
{
    Task<DateTimeOffset?> ObtenerProximoCierreAsync(
        CancellationToken cancellationToken = default);
}

public static class CalculadorProximaRevision
{
    public static DateTimeOffset? SeleccionarProximoCierre(
        IEnumerable<DateTimeOffset> cierres,
        DateTimeOffset ahora)
    {
        return cierres
            .Where(x => x > ahora)
            .OrderBy(x => x)
            .Select(x => (DateTimeOffset?)x)
            .FirstOrDefault();
    }
}
