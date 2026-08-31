using System.Net;
using Temperatura.Web.Domain;
using Temperatura.Web.Domain.Enums;
using Temperatura.Web.Services;

namespace Temperatura.Tests;

public class CorreoAlertaFueraRangoTests
{
    [Fact]
    public void IncluyeSoloMedicionesFueraDeRangoYLosDatosDelRegistro()
    {
        var alerta = CrearAlerta(
            new DetalleRegistro
            {
                Valor = 31.5m,
                LimiteMinimoAplicado = 15m,
                LimiteMaximoAplicado = 30m,
                EstadoRango = EstadoRango.PorEncima,
                Observacion = "Revisar <equipo>",
                TipoMedicion = new TipoMedicion
                {
                    Nombre = "Temperatura ambiental",
                    SimboloUnidad = "°C"
                }
            },
            new DetalleRegistro
            {
                Valor = 50m,
                LimiteMinimoAplicado = 15m,
                LimiteMaximoAplicado = 65m,
                EstadoRango = EstadoRango.DentroDeRango,
                TipoMedicion = new TipoMedicion
                {
                    Nombre = "Humedad relativa",
                    SimboloUnidad = "%"
                }
            });

        var correo = CorreoAlertaFueraRango.Crear(alerta);
        var cuerpoDecodificado = WebUtility.HtmlDecode(correo.CuerpoHtml);

        Assert.Equal("Alerta de medición fuera de rango - Farmacia", correo.Asunto);
        Assert.Contains("Temperatura ambiental", cuerpoDecodificado);
        Assert.Contains("31.5 °C", cuerpoDecodificado);
        Assert.Contains("15–30 °C", cuerpoDecodificado);
        Assert.Contains("Por encima del máximo", cuerpoDecodificado);
        Assert.Contains("Revisar &lt;equipo&gt;", correo.CuerpoHtml);
        Assert.Contains("María Registradora", cuerpoDecodificado);
        Assert.DoesNotContain("Humedad relativa", cuerpoDecodificado);
    }

    [Fact]
    public void RechazaCorreoCuandoNoExistenMedicionesFueraDeRango()
    {
        var alerta = CrearAlerta(new DetalleRegistro
        {
            Valor = 20m,
            LimiteMinimoAplicado = 15m,
            LimiteMaximoAplicado = 30m,
            EstadoRango = EstadoRango.DentroDeRango,
            TipoMedicion = new TipoMedicion
            {
                Nombre = "Temperatura ambiental",
                SimboloUnidad = "°C"
            }
        });

        var exception = Assert.Throws<InvalidOperationException>(
            () => CorreoAlertaFueraRango.Crear(alerta));

        Assert.Contains("no contiene mediciones fuera", exception.Message);
    }

    private static AlertaRegistroFueraRango CrearAlerta(params DetalleRegistro[] detalles) =>
        new()
        {
            Registro = new Registro
            {
                Ambiente = new Ambiente { Nombre = "Farmacia" },
                FechaOperativa = new DateOnly(2026, 8, 31),
                HorarioNombreAplicado = "07:00",
                FechaHoraRegistro = new DateTimeOffset(
                    2026,
                    8,
                    31,
                    7,
                    5,
                    0,
                    TimeSpan.FromHours(-5)),
                Usuario = new ApplicationUser { Nombre = "María Registradora" },
                Detalles = detalles
            }
        };
}
