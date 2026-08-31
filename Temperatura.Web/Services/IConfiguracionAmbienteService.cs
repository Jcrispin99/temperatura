using System.ComponentModel.DataAnnotations;
using Temperatura.Web.Domain.Enums;

namespace Temperatura.Web.Services;

public interface IConfiguracionAmbienteService
{
    Task<ResultadoConsultaConfiguracionAmbiente> ObtenerAsync(
        int ambienteId,
        CancellationToken cancellationToken = default);

    Task<ResultadoActualizacionAmbiente> ActualizarAsync(
        AmbienteConfiguracionInput input,
        CancellationToken cancellationToken = default);
}

public sealed record ResultadoConsultaConfiguracionAmbiente(
    bool Encontrado,
    AmbienteConfiguracionInput Input,
    IReadOnlyList<HorarioAmbienteHistorico> HistorialHorarios);

public sealed record ResultadoActualizacionAmbiente(
    bool Encontrado,
    bool Guardado,
    AmbienteConfiguracionInput Input,
    IReadOnlyList<HorarioAmbienteHistorico> HistorialHorarios,
    IReadOnlyList<ErrorCasoUso> Errores,
    string? NombreAmbiente);

public sealed class AmbienteConfiguracionInput
{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "Ingresa el nombre.")]
    [StringLength(100, ErrorMessage = "El nombre admite hasta 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public List<MedicionAmbienteInput> Mediciones { get; set; } = [];

    public List<HorarioAmbienteInput> Horarios { get; set; } = [];
}

public sealed class MedicionAmbienteInput
{
    public int TipoMedicionId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Unidad { get; set; } = string.Empty;

    public bool Habilitada { get; set; }

    public decimal? RangoMinimo { get; set; }

    public decimal? RangoMaximo { get; set; }
}

public sealed class HorarioAmbienteInput
{
    public int HorarioId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public MomentoOperativo MomentoOperativo { get; set; }

    public bool EsCierreDiaOperativoAnterior { get; set; }

    public bool Habilitado { get; set; }

    public short? MinutosAntes { get; set; }

    public short? MinutosToleranciaPuntualidad { get; set; }

    public short? MinutosDespues { get; set; }

    public short? MinutosRegularizacion { get; set; }
}

public sealed record HorarioAmbienteHistorico(
    string Horario,
    short MinutosAntes,
    short MinutosToleranciaPuntualidad,
    short MinutosDespues,
    short MinutosRegularizacion,
    DateOnly VigenteDesde,
    DateOnly? VigenteHasta);
