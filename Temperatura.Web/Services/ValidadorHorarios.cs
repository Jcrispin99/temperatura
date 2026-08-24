namespace Temperatura.Web.Services;

/// <summary>
/// Vista mínima de un horario del catálogo para validar el conjunto completo.
/// </summary>
public sealed record HorarioValidable(
    int Id,
    TimeOnly HoraReferencia,
    bool EsCierreDiaOperativoAnterior,
    bool Activo);

public sealed record ErrorHorario(string Clave, string Mensaje);

/// <summary>
/// Reglas del catálogo global de horarios. Se validan sobre el conjunto resultante
/// (los existentes más el candidato) porque las restricciones son entre horarios,
/// no de un horario aislado.
/// </summary>
public static class ValidadorHorarios
{
    public const string ClaveHora = "Input.HoraReferencia";
    public const string ClaveCierre = "Input.EsCierreDiaOperativoAnterior";
    public const string ClaveActivo = "Input.Activo";

    public static IReadOnlyList<ErrorHorario> Validar(
        HorarioValidable candidato,
        IEnumerable<HorarioValidable> existentes)
    {
        var errores = new List<ErrorHorario>();
        var otros = existentes.Where(x => x.Id != candidato.Id).ToList();

        if (otros.Any(x => x.HoraReferencia == candidato.HoraReferencia))
        {
            errores.Add(new ErrorHorario(
                ClaveHora,
                $"Ya existe un horario a las {candidato.HoraReferencia:HH\\:mm}. Cada ronda debe tener una hora distinta."));
        }

        var activos = otros.Where(x => x.Activo).ToList();
        if (candidato.Activo)
        {
            activos.Add(candidato);
        }

        var primeraDelDia = activos
            .Where(x => !x.EsCierreDiaOperativoAnterior)
            .OrderBy(x => x.HoraReferencia)
            .FirstOrDefault();

        if (primeraDelDia is null)
        {
            errores.Add(new ErrorHorario(
                candidato.EsCierreDiaOperativoAnterior ? ClaveCierre : ClaveActivo,
                "Debe quedar al menos un horario activo que no cierre el día operativo anterior; " +
                "de lo contrario el sistema se queda sin primera ronda del día."));
            return errores;
        }

        // Las rondas que cierran el día anterior ocurren en la madrugada del día
        // siguiente: si no son anteriores a la primera ronda, los días operativos
        // se solapan y las lecturas se atribuyen al día equivocado.
        var solapadas = activos
            .Where(x => x.EsCierreDiaOperativoAnterior &&
                        x.HoraReferencia >= primeraDelDia.HoraReferencia)
            .ToList();

        if (solapadas.Count > 0)
        {
            errores.Add(new ErrorHorario(
                ClaveCierre,
                $"Un horario que cierra el día operativo anterior debe ser anterior a la primera ronda " +
                $"del día ({primeraDelDia.HoraReferencia:HH\\:mm}). Revisa " +
                string.Join(", ", solapadas.Select(x => x.HoraReferencia.ToString("HH\\:mm"))) + "."));
        }

        return errores;
    }

    /// <summary>Nombre de display por defecto cuando el usuario no escribe uno.</summary>
    public static string NombrePredeterminado(TimeOnly hora) => hora.ToString("HH\\:mm");
}
