using Temperatura.Web.Services;

namespace Temperatura.Tests;

public class AvanceDiarioCalculatorTests
{
    private static readonly DateTimeOffset Apertura =
        new(2026, 8, 12, 6, 30, 0, TimeSpan.FromHours(-5));

    private static readonly DateTimeOffset Cierre =
        new(2026, 8, 12, 8, 0, 0, TimeSpan.FromHours(-5));

    [Fact]
    public void RegistroGuardadoSiempreEstaCompletado()
    {
        var estado = AvanceDiarioCalculator.ObtenerEstado(
            true,
            Apertura.AddHours(-2),
            Apertura,
            Cierre);

        Assert.Equal(EstadoHorarioDiario.Completado, estado);
    }

    [Theory]
    [InlineData(-1, EstadoHorarioDiario.Proximo)]
    [InlineData(0, EstadoHorarioDiario.Pendiente)]
    [InlineData(89, EstadoHorarioDiario.Pendiente)]
    [InlineData(90, EstadoHorarioDiario.Vencido)]
    public void ClasificaHorarioSegunSuVentana(int minutosDesdeApertura, EstadoHorarioDiario esperado)
    {
        var estado = AvanceDiarioCalculator.ObtenerEstado(
            false,
            Apertura.AddMinutes(minutosDesdeApertura),
            Apertura,
            Cierre);

        Assert.Equal(esperado, estado);
    }

    [Fact]
    public void CalculaPorcentajeConDecimales()
    {
        Assert.Equal(66.67m, AvanceDiarioCalculator.CalcularPorcentaje(2, 3));
        Assert.Equal(0m, AvanceDiarioCalculator.CalcularPorcentaje(0, 0));
    }

    [Fact]
    public void AntesDeLaPrimeraAperturaConservaElDiaOperativoAnterior()
    {
        var ahora = new DateTimeOffset(2026, 8, 12, 1, 0, 0, TimeSpan.FromHours(-5));

        var fecha = AvanceDiarioCalculator.DeterminarFechaOperativa(ahora, Apertura);

        Assert.Equal(new DateOnly(2026, 8, 11), fecha);
    }

    [Fact]
    public void AlAbrirLaPrimeraVentanaIniciaElNuevoDiaOperativo()
    {
        var fecha = AvanceDiarioCalculator.DeterminarFechaOperativa(Apertura, Apertura);

        Assert.Equal(new DateOnly(2026, 8, 12), fecha);
    }
}
