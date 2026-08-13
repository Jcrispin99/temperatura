namespace Temperatura.Web.Services;

public static class DetectorRegistrosOmitidos
{
    public static IReadOnlyList<RegistroEsperado> Detectar(
        IEnumerable<RegistroEsperado> esperados,
        IReadOnlySet<RegistroRealizado> realizados,
        IReadOnlySet<RegistroRealizado> alertados,
        DateTimeOffset ahora)
    {
        return esperados
            .Where(x => x.FechaHoraCierre <= ahora)
            .Where(x => !realizados.Contains(new RegistroRealizado(
                x.FechaOperativa,
                x.AmbienteId,
                x.HorarioId)))
            .Where(x => !alertados.Contains(new RegistroRealizado(
                x.FechaOperativa,
                x.AmbienteId,
                x.HorarioId)))
            .OrderBy(x => x.FechaHoraCierre)
            .ThenBy(x => x.Ambiente)
            .ToArray();
    }
}

public sealed record RegistroEsperado(
    DateOnly FechaOperativa,
    int AmbienteId,
    string Ambiente,
    int HorarioId,
    string Horario,
    DateTimeOffset FechaHoraCierre);

public readonly record struct RegistroRealizado(
    DateOnly FechaOperativa,
    int AmbienteId,
    int HorarioId);
