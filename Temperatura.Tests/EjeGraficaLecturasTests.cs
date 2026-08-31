using Temperatura.Web.Domain.Enums;
using Temperatura.Web.Services;

namespace Temperatura.Tests;

public class EjeGraficaLecturasTests
{
    [Fact]
    public void CreaCuatroMomentosPorCadaDiaDelRango()
    {
        var columnas = EjeGraficaLecturas.Crear(
            new DateOnly(2026, 8, 24),
            new DateOnly(2026, 8, 25));

        Assert.Equal(8, columnas.Count);
        Assert.Equal(8, columnas.Select(x => x.Clave).Distinct().Count());
        Assert.Equal(
            ["Mañana", "Mediodía", "Noche", "Medianoche (+1)"],
            columnas.Take(4).Select(x => x.Momento));
        Assert.Equal(
            ["Mañ.", "Tard.", "Noch.", "Mnoch."],
            columnas.Take(4).Select(x => x.MomentoCorto));
    }

    [Theory]
    [InlineData(MomentoOperativo.Manana, "2026-08-24|1")]
    [InlineData(MomentoOperativo.Mediodia, "2026-08-24|2")]
    [InlineData(MomentoOperativo.Noche, "2026-08-24|3")]
    [InlineData(MomentoOperativo.Medianoche, "2026-08-24|4")]
    public void ClaveCombinaFechaOperativaYMomento(
        MomentoOperativo momento,
        string claveEsperada)
    {
        var clave = EjeGraficaLecturas.CrearClave(new DateOnly(2026, 8, 24), momento);

        Assert.Equal(claveEsperada, clave);
    }

    [Fact]
    public void MedianocheQuedaEnElDiaOperativoAunqueIndicaDiaCalendarioSiguiente()
    {
        var columna = Assert.Single(
            EjeGraficaLecturas.Crear(
                new DateOnly(2026, 8, 24),
                new DateOnly(2026, 8, 24)),
            x => x.Momento.StartsWith("Medianoche", StringComparison.Ordinal));

        Assert.Equal("2026-08-24|4", columna.Clave);
        Assert.Equal("Medianoche (+1)", columna.Momento);
    }
}
