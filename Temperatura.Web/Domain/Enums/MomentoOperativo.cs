namespace Temperatura.Web.Domain.Enums;

public enum MomentoOperativo
{
    Manana = 1,
    Mediodia = 2,
    Noche = 3,
    Medianoche = 4
}

public static class MomentoOperativoExtensions
{
    public static string ObtenerNombre(this MomentoOperativo momento) => momento switch
    {
        MomentoOperativo.Manana => "Mañana",
        MomentoOperativo.Mediodia => "Mediodía",
        MomentoOperativo.Noche => "Noche",
        MomentoOperativo.Medianoche => "Medianoche",
        _ => "Momento no definido"
    };
}
