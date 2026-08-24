using Microsoft.Extensions.Configuration;
using Temperatura.Web.Domain;
using Temperatura.Web.Domain.Enums;
using Temperatura.Web.Services;

namespace Temperatura.Tests;

public class VentanaRegistroServiceTests
{
    private readonly VentanaRegistroService _service = new(
        TimeProvider.System,
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Sistema:ZonaHoraria"] = "America/Lima"
            })
            .Build());

    [Fact]
    public void AbreTreintaMinutosAntesComoPuntual()
    {
        var configuracion = CrearConfiguracion(new TimeOnly(7, 0));
        var ahora = new DateTimeOffset(2026, 8, 11, 6, 30, 0, TimeSpan.FromHours(-5));

        var ventana = Assert.Single(_service.ObtenerVentanasAbiertas([configuracion], ahora));

        Assert.Equal(new DateOnly(2026, 8, 11), ventana.FechaOperativa);
        Assert.Equal(EstadoPuntualidad.Puntual, ventana.Puntualidad);
    }

    [Fact]
    public void HastaTreintaMinutosDespuesSigueSiendoPuntual()
    {
        var configuracion = CrearConfiguracion(new TimeOnly(7, 0));
        var ahora = new DateTimeOffset(2026, 8, 11, 7, 30, 0, TimeSpan.FromHours(-5));

        var ventana = Assert.Single(_service.ObtenerVentanasAbiertas([configuracion], ahora));

        Assert.Equal(EstadoPuntualidad.Puntual, ventana.Puntualidad);
        Assert.Equal(
            new DateTimeOffset(2026, 8, 11, 7, 30, 0, TimeSpan.FromHours(-5)),
            ventana.LimitePuntualidad);
    }

    [Fact]
    public void DespuesDeLaToleranciaSeMarcaComoTardio()
    {
        var configuracion = CrearConfiguracion(new TimeOnly(7, 0));
        var ahora = new DateTimeOffset(2026, 8, 11, 7, 31, 0, TimeSpan.FromHours(-5));

        var ventana = Assert.Single(_service.ObtenerVentanasAbiertas([configuracion], ahora));

        Assert.Equal(EstadoPuntualidad.Tardio, ventana.Puntualidad);
    }

    [Fact]
    public void RespetaLaToleranciaConfiguradaPorAmbiente()
    {
        var configuracion = CrearConfiguracion(new TimeOnly(11, 0));
        configuracion.MinutosToleranciaPuntualidad = 15;
        var ahora = new DateTimeOffset(2026, 8, 11, 11, 16, 0, TimeSpan.FromHours(-5));

        var ventana = Assert.Single(_service.ObtenerVentanasAbiertas([configuracion], ahora));

        Assert.Equal(EstadoPuntualidad.Tardio, ventana.Puntualidad);
    }

    [Fact]
    public void AlCierrePermiteRegularizarYMarcaFueraDePlazo()
    {
        var configuracion = CrearConfiguracion(new TimeOnly(7, 0));
        var ahora = new DateTimeOffset(2026, 8, 11, 8, 0, 0, TimeSpan.FromHours(-5));

        var ventana = Assert.Single(_service.ObtenerVentanasAbiertas([configuracion], ahora));

        Assert.Equal(EstadoPuntualidad.FueraDePlazo, ventana.Puntualidad);
        Assert.Equal(
            new DateTimeOffset(2026, 8, 11, 20, 0, 0, TimeSpan.FromHours(-5)),
            ventana.FinRegularizacion);
    }

    [Fact]
    public void BloqueaCuandoTerminaElPlazoDeRegularizacion()
    {
        var configuracion = CrearConfiguracion(new TimeOnly(7, 0));
        var ahora = new DateTimeOffset(2026, 8, 11, 20, 0, 0, TimeSpan.FromHours(-5));

        var ventanas = _service.ObtenerVentanasAbiertas([configuracion], ahora);

        Assert.Empty(ventanas);
    }

    [Fact]
    public void RespetaPlazoDeRegularizacionAdministrable()
    {
        var configuracion = CrearConfiguracion(new TimeOnly(11, 0));
        configuracion.MinutosRegularizacion = 30;
        var dentro = new DateTimeOffset(2026, 8, 11, 12, 29, 0, TimeSpan.FromHours(-5));
        var fuera = new DateTimeOffset(2026, 8, 11, 12, 30, 0, TimeSpan.FromHours(-5));

        Assert.Equal(
            EstadoPuntualidad.FueraDePlazo,
            Assert.Single(_service.ObtenerVentanasAbiertas([configuracion], dentro)).Puntualidad);
        Assert.Empty(_service.ObtenerVentanasAbiertas([configuracion], fuera));
    }

    [Fact]
    public void DevuelveCadaFechaOperativaPendienteAunqueCompartaElMismoHorario()
    {
        var configuracion = CrearConfiguracion(new TimeOnly(7, 0));
        configuracion.MinutosRegularizacion = 2880;
        var ahora = new DateTimeOffset(2026, 8, 24, 7, 0, 0, TimeSpan.FromHours(-5));

        var ventanas = _service.ObtenerVentanasAbiertas([configuracion], ahora);

        Assert.Equal(3, ventanas.Count);
        Assert.Contains(ventanas, x =>
            x.FechaOperativa == new DateOnly(2026, 8, 24) &&
            x.Puntualidad == EstadoPuntualidad.Puntual);
        Assert.Contains(ventanas, x =>
            x.FechaOperativa == new DateOnly(2026, 8, 23) &&
            x.Puntualidad == EstadoPuntualidad.FueraDePlazo);
        Assert.Contains(ventanas, x =>
            x.FechaOperativa == new DateOnly(2026, 8, 22) &&
            x.Puntualidad == EstadoPuntualidad.FueraDePlazo);
    }

    [Fact]
    public void MedianochePerteneceAlDiaOperativoAnterior()
    {
        var configuracion = CrearConfiguracion(
            new TimeOnly(1, 0),
            esCierreDiaOperativoAnterior: true);
        var ahora = new DateTimeOffset(2026, 8, 12, 1, 30, 0, TimeSpan.FromHours(-5));

        var ventana = Assert.Single(_service.ObtenerVentanasAbiertas([configuracion], ahora));

        Assert.Equal(new DateOnly(2026, 8, 11), ventana.FechaOperativa);
        Assert.Equal(EstadoPuntualidad.Puntual, ventana.Puntualidad);
    }

    [Fact]
    public void MedianocheUsaLaConfiguracionHistoricaDelDiaOperativoAnterior()
    {
        var historica = CrearConfiguracion(
            new TimeOnly(1, 0),
            esCierreDiaOperativoAnterior: true);
        historica.Activo = false;
        historica.VigenteHasta = new DateOnly(2026, 8, 11);

        var actual = CrearConfiguracion(
            new TimeOnly(1, 0),
            esCierreDiaOperativoAnterior: true);
        actual.Id = 2;
        actual.MinutosToleranciaPuntualidad = 15;
        actual.MinutosDespues = 15;
        actual.VigenteDesde = new DateOnly(2026, 8, 12);

        var ahora = new DateTimeOffset(2026, 8, 12, 1, 30, 0, TimeSpan.FromHours(-5));

        var ventana = Assert.Single(_service.ObtenerVentanasAbiertas([historica, actual], ahora));

        Assert.Equal(historica.Id, ventana.Configuracion.Id);
        Assert.Equal(new DateOnly(2026, 8, 11), ventana.FechaOperativa);
    }

    private static AmbienteHorario CrearConfiguracion(
        TimeOnly hora,
        bool esCierreDiaOperativoAnterior = false)
    {
        return new AmbienteHorario
        {
            Id = 1,
            AmbienteId = 1,
            HorarioId = 1,
            MinutosAntes = 30,
            MinutosToleranciaPuntualidad = 30,
            MinutosDespues = 60,
            MinutosRegularizacion = 720,
            VigenteDesde = new DateOnly(2026, 1, 1),
            Activo = true,
            Horario = new Horario
            {
                Id = 1,
                Nombre = hora.ToString("HH:mm"),
                HoraReferencia = hora,
                EsCierreDiaOperativoAnterior = esCierreDiaOperativoAnterior,
                Activo = true
            }
        };
    }
}
