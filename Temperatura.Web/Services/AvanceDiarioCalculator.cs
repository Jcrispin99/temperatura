namespace Temperatura.Web.Services;

public static class AvanceDiarioCalculator
{
    public static DateOnly DeterminarFechaOperativa(
        DateTimeOffset ahora,
        DateTimeOffset primeraAperturaDelDia)
    {
        var fechaLocal = DateOnly.FromDateTime(ahora.DateTime);
        return ahora >= primeraAperturaDelDia ? fechaLocal : fechaLocal.AddDays(-1);
    }

    public static EstadoHorarioDiario ObtenerEstado(
        bool completado,
        DateTimeOffset ahora,
        DateTimeOffset apertura,
        DateTimeOffset cierre)
    {
        if (completado)
        {
            return EstadoHorarioDiario.Completado;
        }

        if (ahora < apertura)
        {
            return EstadoHorarioDiario.Proximo;
        }

        return ahora < cierre
            ? EstadoHorarioDiario.Pendiente
            : EstadoHorarioDiario.Vencido;
    }

    public static decimal CalcularPorcentaje(int completados, int esperados)
    {
        return esperados == 0
            ? 0m
            : decimal.Round(completados * 100m / esperados, 2);
    }
}
