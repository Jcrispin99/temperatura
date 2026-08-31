using System.Globalization;
using System.Net;
using Temperatura.Web.Domain;
using Temperatura.Web.Domain.Enums;

namespace Temperatura.Web.Services;

public static class CorreoAlertaFueraRango
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-PE");

    public static ContenidoCorreoAlertaRango Crear(AlertaRegistroFueraRango alerta)
    {
        var registro = alerta.Registro;
        var detalles = registro.Detalles
            .Where(x => x.EstadoRango != EstadoRango.DentroDeRango)
            .OrderBy(x => x.TipoMedicion.Nombre)
            .ToArray();

        if (detalles.Length == 0)
        {
            throw new InvalidOperationException(
                "El registro no contiene mediciones fuera del rango permitido.");
        }

        var filas = string.Join(string.Empty, detalles.Select(detalle =>
            $"<tr><td>{Codificar(detalle.TipoMedicion.Nombre)}</td>" +
            $"<td>{Formatear(detalle.Valor)} {Codificar(detalle.TipoMedicion.SimboloUnidad)}</td>" +
            $"<td>{Formatear(detalle.LimiteMinimoAplicado)}–{Formatear(detalle.LimiteMaximoAplicado)} " +
            $"{Codificar(detalle.TipoMedicion.SimboloUnidad)}</td>" +
            $"<td>{Codificar(EtiquetaEstado(detalle.EstadoRango))}</td>" +
            $"<td>{Codificar(detalle.Observacion ?? "—")}</td></tr>"));

        var asunto = $"Alerta de medición fuera de rango - {registro.Ambiente.Nombre}";
        var cuerpo = $"""
            <h2>Medición fuera del rango permitido</h2>
            <p>Se confirmó un registro con una o más mediciones fuera de los límites establecidos.</p>
            <p>
              <strong>Ambiente:</strong> {Codificar(registro.Ambiente.Nombre)}<br>
              <strong>Fecha operativa:</strong> {registro.FechaOperativa:dd/MM/yyyy}<br>
              <strong>Horario:</strong> {Codificar(registro.HorarioNombreAplicado)}<br>
              <strong>Registrado:</strong> {registro.FechaHoraRegistro:dd/MM/yyyy HH:mm}<br>
              <strong>Usuario:</strong> {Codificar(registro.Usuario.Nombre)}
            </p>
            <table style="border-collapse:collapse" border="1" cellpadding="8">
              <thead><tr><th>Medición</th><th>Valor registrado</th><th>Rango permitido</th><th>Resultado</th><th>Observación</th></tr></thead>
              <tbody>{filas}</tbody>
            </table>
            <p>Por favor, revisa el registro y realiza la acción correctiva que corresponda.</p>
            """;

        return new ContenidoCorreoAlertaRango(asunto, cuerpo);
    }

    private static string Formatear(decimal valor) => valor.ToString("0.##", Cultura);

    private static string Codificar(string valor) => WebUtility.HtmlEncode(valor);

    private static string EtiquetaEstado(EstadoRango estado) => estado switch
    {
        EstadoRango.PorDebajo => "Por debajo del mínimo",
        EstadoRango.PorEncima => "Por encima del máximo",
        _ => "Dentro del rango"
    };
}

public sealed record ContenidoCorreoAlertaRango(string Asunto, string CuerpoHtml);
