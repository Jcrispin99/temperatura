using Temperatura.Web.Services;

namespace Temperatura.Tests;

public class DetectorRegistrosOmitidosTests
{
    private static readonly DateTimeOffset Ahora =
        new(2026, 8, 12, 8, 5, 0, TimeSpan.FromHours(-5));

    [Fact]
    public void DetectaAmbienteSinRegistroDespuesDelCierre()
    {
        var esperado = CrearEsperado(Ahora.AddMinutes(-5));

        var resultado = DetectorRegistrosOmitidos.Detectar(
            [esperado],
            new HashSet<RegistroRealizado>(),
            new HashSet<RegistroRealizado>(),
            Ahora);

        Assert.Single(resultado);
        Assert.Equal("Farmacia", resultado[0].Ambiente);
    }

    [Fact]
    public void NoAlertaAntesDelCierre()
    {
        var esperado = CrearEsperado(Ahora.AddMinutes(5));

        var resultado = DetectorRegistrosOmitidos.Detectar(
            [esperado],
            new HashSet<RegistroRealizado>(),
            new HashSet<RegistroRealizado>(),
            Ahora);

        Assert.Empty(resultado);
    }

    [Fact]
    public void NoAlertaCuandoElRegistroExiste()
    {
        var esperado = CrearEsperado(Ahora.AddMinutes(-5));
        var clave = new RegistroRealizado(
            esperado.FechaOperativa,
            esperado.AmbienteId,
            esperado.HorarioId);

        var resultado = DetectorRegistrosOmitidos.Detectar(
            [esperado],
            new HashSet<RegistroRealizado> { clave },
            new HashSet<RegistroRealizado>(),
            Ahora);

        Assert.Empty(resultado);
    }

    [Fact]
    public void NoGeneraLaMismaAlertaDosVeces()
    {
        var esperado = CrearEsperado(Ahora.AddMinutes(-5));
        var clave = new RegistroRealizado(
            esperado.FechaOperativa,
            esperado.AmbienteId,
            esperado.HorarioId);

        var resultado = DetectorRegistrosOmitidos.Detectar(
            [esperado],
            new HashSet<RegistroRealizado>(),
            new HashSet<RegistroRealizado> { clave },
            Ahora);

        Assert.Empty(resultado);
    }

    private static RegistroEsperado CrearEsperado(DateTimeOffset cierre)
    {
        return new RegistroEsperado(
            new DateOnly(2026, 8, 12),
            1,
            "Farmacia",
            1,
            "07:00",
            cierre);
    }
}
