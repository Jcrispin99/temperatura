using System.Globalization;
using Temperatura.Web.Domain.Enums;

namespace Temperatura.Web.Services;

public static class EjeGraficaLecturas
{
    public const int MaximoDias = 31;

    private static readonly CultureInfo CulturaEspanol = CultureInfo.GetCultureInfo("es-PE");

    public static IReadOnlyList<ColumnaGraficaLectura> Crear(DateOnly fechaDesde, DateOnly fechaHasta)
    {
        var columnas = new List<ColumnaGraficaLectura>();
        for (var fecha = fechaDesde; fecha <= fechaHasta; fecha = fecha.AddDays(1))
        {
            var etiquetaFecha = fecha
                .ToDateTime(TimeOnly.MinValue)
                .ToString("ddd dd/MM", CulturaEspanol);

            foreach (var momento in Enum.GetValues<MomentoOperativo>())
            {
                columnas.Add(new ColumnaGraficaLectura(
                    CrearClave(fecha, momento),
                    etiquetaFecha,
                    momento == MomentoOperativo.Medianoche
                        ? "Medianoche (+1)"
                        : momento.ObtenerNombre(),
                    ObtenerNombreCorto(momento)));
            }
        }

        return columnas;
    }

    public static string CrearClave(DateOnly fecha, MomentoOperativo momento) =>
        $"{fecha:yyyy-MM-dd}|{(int)momento}";

    private static string ObtenerNombreCorto(MomentoOperativo momento) => momento switch
    {
        MomentoOperativo.Manana => "Mañ.",
        MomentoOperativo.Mediodia => "Tard.",
        MomentoOperativo.Noche => "Noch.",
        MomentoOperativo.Medianoche => "Mnoch.",
        _ => string.Empty
    };
}

public sealed record ColumnaGraficaLectura(
    string Clave,
    string Fecha,
    string Momento,
    string MomentoCorto);
