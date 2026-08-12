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
    public void DespuesDeLaHoraSeMarcaComoTardio()
    {
        var configuracion = CrearConfiguracion(new TimeOnly(7, 0));
        var ahora = new DateTimeOffset(2026, 8, 11, 7, 30, 0, TimeSpan.FromHours(-5));

        var ventana = Assert.Single(_service.ObtenerVentanasAbiertas([configuracion], ahora));

        Assert.Equal(EstadoPuntualidad.Tardio, ventana.Puntualidad);
    }

    [Fact]
    public void BloqueaAlCumplirseUnaHora()
    {
        var configuracion = CrearConfiguracion(new TimeOnly(7, 0));
        var ahora = new DateTimeOffset(2026, 8, 11, 8, 0, 0, TimeSpan.FromHours(-5));

        var ventanas = _service.ObtenerVentanasAbiertas([configuracion], ahora);

        Assert.Empty(ventanas);
    }

    [Fact]
    public void MedianochePerteneceAlDiaOperativoAnterior()
    {
        var configuracion = CrearConfiguracion(
            new TimeOnly(0, 0),
            esCierreDiaOperativoAnterior: true);
        var ahora = new DateTimeOffset(2026, 8, 12, 0, 30, 0, TimeSpan.FromHours(-5));

        var ventana = Assert.Single(_service.ObtenerVentanasAbiertas([configuracion], ahora));

        Assert.Equal(new DateOnly(2026, 8, 11), ventana.FechaOperativa);
        Assert.Equal(EstadoPuntualidad.Tardio, ventana.Puntualidad);
    }

    [Fact]
    public void MedianocheUsaLaConfiguracionHistoricaDelDiaOperativoAnterior()
    {
        var historica = CrearConfiguracion(
            new TimeOnly(0, 0),
            esCierreDiaOperativoAnterior: true);
        historica.Activo = false;
        historica.VigenteHasta = new DateOnly(2026, 8, 11);

        var actual = CrearConfiguracion(
            new TimeOnly(0, 0),
            esCierreDiaOperativoAnterior: true);
        actual.Id = 2;
        actual.MinutosDespues = 15;
        actual.VigenteDesde = new DateOnly(2026, 8, 12);

        var ahora = new DateTimeOffset(2026, 8, 12, 0, 30, 0, TimeSpan.FromHours(-5));

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
            MinutosDespues = 60,
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
