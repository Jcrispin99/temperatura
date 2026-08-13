using Temperatura.Web.Services;

namespace Temperatura.Tests;

public class CalculadorProximaRevisionTests
{
    private static readonly DateTimeOffset Ahora =
        new(2026, 8, 12, 10, 0, 0, TimeSpan.FromHours(-5));

    [Fact]
    public void SeleccionaElCierreFuturoMasCercano()
    {
        var cierre = CalculadorProximaRevision.SeleccionarProximoCierre(
            [Ahora.AddHours(10), Ahora.AddHours(3), Ahora.AddHours(17)],
            Ahora);

        Assert.Equal(Ahora.AddHours(3), cierre);
    }

    [Fact]
    public void IgnoraCierresQueYaOcurrieron()
    {
        var cierre = CalculadorProximaRevision.SeleccionarProximoCierre(
            [Ahora.AddMinutes(-1), Ahora, Ahora.AddHours(10)],
            Ahora);

        Assert.Equal(Ahora.AddHours(10), cierre);
    }

    [Fact]
    public void RetornaNuloCuandoNoExistenCierresFuturos()
    {
        var cierre = CalculadorProximaRevision.SeleccionarProximoCierre(
            [Ahora.AddHours(-1), Ahora],
            Ahora);

        Assert.Null(cierre);
    }
}
