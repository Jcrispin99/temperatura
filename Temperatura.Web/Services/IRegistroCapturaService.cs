using System.ComponentModel.DataAnnotations;
using Temperatura.Web.Domain.Enums;

namespace Temperatura.Web.Services;

public interface IRegistroCapturaService
{
    Task<PreparacionCapturaRegistro> PrepararAsync(
        ContextoCapturaRegistro contexto,
        SeleccionCapturaRegistro seleccion,
        IReadOnlyDictionary<int, decimal?>? valoresEnviados = null,
        IReadOnlyDictionary<int, string?>? observacionesEnviadas = null,
        bool permitirAmbientePredeterminado = false,
        CancellationToken cancellationToken = default);

    Task<ResultadoCapturaRegistro> GuardarAsync(
        ContextoCapturaRegistro contexto,
        SolicitudCapturaRegistro solicitud,
        CancellationToken cancellationToken = default);
}

public sealed record ContextoCapturaRegistro(string UsuarioId, bool EsSupervisor);

public sealed record SeleccionCapturaRegistro(
    int? AmbienteId,
    int HorarioId,
    DateOnly? FechaOperativa);

public sealed record SolicitudCapturaRegistro(
    int? AmbienteId,
    int HorarioId,
    DateOnly? FechaOperativa,
    IReadOnlyCollection<MedicionCapturaInput> Mediciones,
    bool ConfirmacionFueraDeRango,
    string? MotivoFueraDePlazo);

public sealed record PreparacionCapturaRegistro(
    bool Autorizado,
    int? AmbienteId,
    int HorarioId,
    DateOnly? FechaOperativa,
    IReadOnlyList<AmbienteCapturaOpcion> Ambientes,
    IReadOnlyList<HorarioCapturaOpcion> HorariosDisponibles,
    List<MedicionCapturaInput> Mediciones,
    HorarioCapturaOpcion? HorarioSeleccionado,
    string? AmbienteSeleccionado,
    string? MensajeSinVentana);

public sealed record ResultadoCapturaRegistro(
    PreparacionCapturaRegistro Preparacion,
    IReadOnlyList<ErrorCasoUso> Errores,
    string? MotivoFueraDePlazo,
    bool Guardado,
    string? MensajeExito);

public sealed class MedicionCapturaInput
{
    public int AmbienteMedicionId { get; set; }

    public int TipoMedicionId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Unidad { get; set; } = string.Empty;

    public byte DecimalesPermitidos { get; set; }

    public decimal RangoMinimo { get; set; }

    public decimal RangoMaximo { get; set; }

    [Required(ErrorMessage = "Ingresa un valor.")]
    public decimal? Valor { get; set; }

    public string? Observacion { get; set; }
}

public sealed record AmbienteCapturaOpcion(int Id, string Nombre, bool EsPredeterminado);

public sealed record HorarioCapturaOpcion(
    int HorarioId,
    string Nombre,
    TimeOnly HoraReferencia,
    MomentoOperativo MomentoOperativo,
    bool EsCierreDiaOperativoAnterior,
    DateOnly FechaOperativa,
    DateTimeOffset Apertura,
    DateTimeOffset LimitePuntualidad,
    DateTimeOffset Cierre,
    DateTimeOffset FinRegularizacion,
    EstadoPuntualidad Puntualidad);
