using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Data;
using Temperatura.Web.Domain;
using Temperatura.Web.Domain.Enums;

namespace Temperatura.Web.Services;

public sealed class RegistroCapturaService(
    ApplicationDbContext context,
    IVentanaRegistroService ventanaRegistroService,
    IAlertaRegistroFueraRangoService alertaRegistroFueraRangoService,
    ILogger<RegistroCapturaService> logger) : IRegistroCapturaService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IVentanaRegistroService _ventanaRegistroService = ventanaRegistroService;
    private readonly IAlertaRegistroFueraRangoService _alertaRegistroFueraRangoService =
        alertaRegistroFueraRangoService;
    private readonly ILogger<RegistroCapturaService> _logger = logger;

    public async Task<PreparacionCapturaRegistro> PrepararAsync(
        ContextoCapturaRegistro contexto,
        SeleccionCapturaRegistro seleccion,
        IReadOnlyDictionary<int, decimal?>? valoresEnviados = null,
        IReadOnlyDictionary<int, string?>? observacionesEnviadas = null,
        bool permitirAmbientePredeterminado = false,
        CancellationToken cancellationToken = default)
    {
        var ambientes = await ObtenerAmbientesAutorizadosAsync(contexto, cancellationToken);
        if (ambientes.Count == 0)
        {
            return CrearPreparacion(
                true,
                seleccion,
                ambientes,
                mensajeSinVentana: "No tienes ambientes asignados para registrar.");
        }

        var ambienteId = seleccion.AmbienteId;
        if (ambienteId is null && permitirAmbientePredeterminado)
        {
            ambienteId = ambientes.FirstOrDefault(x => x.EsPredeterminado)?.Id ?? ambientes[0].Id;
        }

        var ambiente = ambientes.FirstOrDefault(x => x.Id == ambienteId);
        if (ambiente is null)
        {
            return CrearPreparacion(
                false,
                seleccion with { AmbienteId = ambienteId },
                ambientes);
        }

        var ahoraLocal = _ventanaRegistroService.ObtenerAhoraLocal();
        var fechaLocal = DateOnly.FromDateTime(ahoraLocal.DateTime);
        var fechaOperativaMinima = fechaLocal.AddDays(-3);
        var configuracionesHorario = await _context.AmbientesHorarios
            .AsNoTracking()
            .Include(x => x.Horario)
            .Where(x =>
                x.AmbienteId == ambiente.Id &&
                x.Horario.Activo &&
                x.VigenteDesde <= fechaLocal &&
                (x.VigenteHasta == null || x.VigenteHasta >= fechaOperativaMinima))
            .ToListAsync(cancellationToken);

        var ventanas = _ventanaRegistroService.ObtenerVentanasAbiertas(configuracionesHorario, ahoraLocal);
        var fechasOperativas = ventanas.Select(x => x.FechaOperativa).Distinct().ToArray();
        var horariosRegistrados = fechasOperativas.Length == 0
            ? []
            : await _context.Registros
                .AsNoTracking()
                .Where(x => x.AmbienteId == ambiente.Id && fechasOperativas.Contains(x.FechaOperativa))
                .Select(x => new RegistroExistente(x.FechaOperativa, x.HorarioId))
                .ToListAsync(cancellationToken);

        var existentes = horariosRegistrados
            .Select(x => (x.FechaOperativa, x.HorarioId))
            .ToHashSet();

        var horariosDisponibles = ventanas
            .Where(x => !existentes.Contains((x.FechaOperativa, x.Configuracion.HorarioId)))
            .OrderBy(x => x.Puntualidad == EstadoPuntualidad.FueraDePlazo ? 1 : 0)
            .ThenByDescending(x => x.HoraReferencia)
            .Select(x => new HorarioCapturaOpcion(
                x.Configuracion.HorarioId,
                x.Configuracion.Horario.Nombre,
                x.Configuracion.Horario.HoraReferencia,
                x.Configuracion.Horario.MomentoOperativo,
                x.Configuracion.Horario.EsCierreDiaOperativoAnterior,
                x.FechaOperativa,
                x.Apertura,
                x.LimitePuntualidad,
                x.Cierre,
                x.FinRegularizacion,
                x.Puntualidad))
            .ToArray();

        var mensajeSinVentana = horariosDisponibles.Length == 0
            ? ventanas.Count == 0
                ? "En este momento no hay un horario abierto para este ambiente."
                : "El registro del horario disponible ya fue completado."
            : null;

        var horarioId = seleccion.HorarioId;
        var fechaOperativa = seleccion.FechaOperativa;
        if (horarioId == 0 && !fechaOperativa.HasValue)
        {
            var rondaActual = horariosDisponibles.FirstOrDefault(x =>
                x.Puntualidad != EstadoPuntualidad.FueraDePlazo);
            if (rondaActual is not null)
            {
                horarioId = rondaActual.HorarioId;
                fechaOperativa = rondaActual.FechaOperativa;
            }
        }

        var horarioSeleccionado = fechaOperativa.HasValue
            ? horariosDisponibles.FirstOrDefault(x =>
                x.HorarioId == horarioId &&
                x.FechaOperativa == fechaOperativa.Value)
            : horariosDisponibles.FirstOrDefault(x =>
                x.HorarioId == horarioId &&
                x.Puntualidad != EstadoPuntualidad.FueraDePlazo);

        var mediciones = horarioSeleccionado is null
            ? []
            : await ObtenerMedicionesAsync(
                ambiente.Id,
                horarioSeleccionado.FechaOperativa,
                valoresEnviados,
                observacionesEnviadas,
                cancellationToken);

        return new PreparacionCapturaRegistro(
            true,
            ambiente.Id,
            horarioId,
            fechaOperativa,
            ambientes,
            horariosDisponibles,
            mediciones,
            horarioSeleccionado,
            ambiente.Nombre,
            mensajeSinVentana);
    }

    public async Task<ResultadoCapturaRegistro> GuardarAsync(
        ContextoCapturaRegistro contexto,
        SolicitudCapturaRegistro solicitud,
        CancellationToken cancellationToken = default)
    {
        var errores = new List<ErrorCasoUso>();
        var medicionesEnviadas = solicitud.Mediciones.ToArray();
        var gruposEnviados = medicionesEnviadas.GroupBy(x => x.AmbienteMedicionId).ToList();
        var valoresPorConfiguracion = gruposEnviados
            .ToDictionary(x => x.Key, x => x.First().Valor);
        var observacionesPorConfiguracion = gruposEnviados
            .ToDictionary(x => x.Key, x => x.First().Observacion);

        if (gruposEnviados.Any(x => x.Count() > 1))
        {
            errores.Add(new ErrorCasoUso(string.Empty, "La solicitud contiene mediciones duplicadas."));
        }

        var preparacion = await PrepararAsync(
            contexto,
            new SeleccionCapturaRegistro(
                solicitud.AmbienteId,
                solicitud.HorarioId,
                solicitud.FechaOperativa),
            valoresPorConfiguracion,
            observacionesPorConfiguracion,
            permitirAmbientePredeterminado: false,
            cancellationToken);

        if (!preparacion.Autorizado ||
            preparacion.HorarioSeleccionado is null ||
            preparacion.AmbienteId is null)
        {
            errores.Add(new ErrorCasoUso(
                string.Empty,
                "El ambiente o el horario ya no está disponible."));
            return CrearResultado(preparacion, errores, solicitud.MotivoFueraDePlazo);
        }

        if (preparacion.Mediciones.Count == 0)
        {
            errores.Add(new ErrorCasoUso(
                string.Empty,
                "El ambiente no tiene mediciones configuradas para esta fecha."));
        }

        var idsEsperados = preparacion.Mediciones.Select(x => x.AmbienteMedicionId).ToHashSet();
        var idsEnviados = medicionesEnviadas.Select(x => x.AmbienteMedicionId).ToHashSet();
        if (!idsEsperados.SetEquals(idsEnviados))
        {
            errores.Add(new ErrorCasoUso(
                string.Empty,
                "Las mediciones enviadas no corresponden al ambiente seleccionado."));
        }

        ValidarMediciones(preparacion.Mediciones, solicitud.ConfirmacionFueraDeRango, errores);

        var motivoFueraDePlazo = preparacion.HorarioSeleccionado.Puntualidad ==
                EstadoPuntualidad.FueraDePlazo &&
            !string.IsNullOrWhiteSpace(solicitud.MotivoFueraDePlazo)
                ? solicitud.MotivoFueraDePlazo.Trim()
                : null;
        ValidarMotivoFueraDePlazo(
            preparacion.HorarioSeleccionado,
            motivoFueraDePlazo,
            errores);

        if (errores.Count > 0)
        {
            return CrearResultado(preparacion, errores, motivoFueraDePlazo);
        }

        var horario = preparacion.HorarioSeleccionado;
        var ahoraLocal = _ventanaRegistroService.ObtenerAhoraLocal();
        var registro = new Registro
        {
            FechaOperativa = horario.FechaOperativa,
            AmbienteId = preparacion.AmbienteId.Value,
            HorarioId = horario.HorarioId,
            HorarioNombreAplicado = horario.Nombre,
            HoraReferenciaAplicada = horario.HoraReferencia,
            MomentoOperativoAplicado = horario.MomentoOperativo,
            EsCierreDiaOperativoAnteriorAplicado = horario.EsCierreDiaOperativoAnterior,
            UsuarioId = contexto.UsuarioId,
            FechaHoraRegistro = ahoraLocal,
            Estado = EstadoRegistro.Confirmado,
            Puntualidad = horario.Puntualidad,
            MotivoFueraDePlazo = motivoFueraDePlazo,
            Detalles = preparacion.Mediciones.Select(x => new DetalleRegistro
            {
                AmbienteMedicionId = x.AmbienteMedicionId,
                TipoMedicionId = x.TipoMedicionId,
                Valor = x.Valor!.Value,
                LimiteMinimoAplicado = x.RangoMinimo,
                LimiteMaximoAplicado = x.RangoMaximo,
                EstadoRango = EvaluarRango(x.Valor.Value, x.RangoMinimo, x.RangoMaximo),
                Observacion = x.Observacion
            }).ToArray()
        };

        _context.Registros.Add(registro);
        if (horario.Puntualidad == EstadoPuntualidad.FueraDePlazo)
        {
            await AsociarRegularizacionAsync(registro, horario, ahoraLocal, cancellationToken);
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(
                exception,
                "No se pudo guardar el registro del ambiente {AmbienteId} y horario {HorarioId}.",
                preparacion.AmbienteId,
                horario.HorarioId);
            errores.Add(new ErrorCasoUso(
                string.Empty,
                "No se pudo guardar. Es posible que este horario ya haya sido registrado."));
            return CrearResultado(preparacion, errores, motivoFueraDePlazo);
        }

        try
        {
            await _alertaRegistroFueraRangoService.RegistrarYNotificarAsync(
                registro.Id,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "El registro {RegistroId} se guardó, pero no se pudo procesar su alerta de rango.",
                registro.Id);
        }

        var mensajeExito = horario.Puntualidad == EstadoPuntualidad.FueraDePlazo
            ? $"Registro de {preparacion.AmbienteSeleccionado} guardado fuera de plazo. La falta quedó pendiente de revisión."
            : $"Registro de {preparacion.AmbienteSeleccionado} guardado correctamente.";

        return new ResultadoCapturaRegistro(
            preparacion,
            [],
            motivoFueraDePlazo,
            true,
            mensajeExito);
    }

    private async Task<List<MedicionCapturaInput>> ObtenerMedicionesAsync(
        int ambienteId,
        DateOnly fechaOperativa,
        IReadOnlyDictionary<int, decimal?>? valoresEnviados,
        IReadOnlyDictionary<int, string?>? observacionesEnviadas,
        CancellationToken cancellationToken)
    {
        var candidatas = await _context.AmbientesMediciones
            .AsNoTracking()
            .Include(x => x.TipoMedicion)
            .Where(x =>
                x.AmbienteId == ambienteId &&
                x.TipoMedicion.Activo &&
                x.VigenteDesde <= fechaOperativa &&
                (x.VigenteHasta == null || x.VigenteHasta >= fechaOperativa))
            .OrderBy(x => x.TipoMedicionId)
            .ToListAsync(cancellationToken);

        return candidatas
            .GroupBy(x => x.TipoMedicionId)
            .Select(x => x
                .OrderByDescending(y => y.Activo)
                .ThenByDescending(y => y.VigenteDesde)
                .ThenByDescending(y => y.Id)
                .First())
            .OrderBy(x => x.TipoMedicionId)
            .Select(x => new MedicionCapturaInput
            {
                AmbienteMedicionId = x.Id,
                TipoMedicionId = x.TipoMedicionId,
                Nombre = x.TipoMedicion.Nombre,
                Unidad = x.TipoMedicion.SimboloUnidad,
                DecimalesPermitidos = x.TipoMedicion.DecimalesPermitidos,
                RangoMinimo = x.RangoMinimo,
                RangoMaximo = x.RangoMaximo,
                Valor = valoresEnviados?.GetValueOrDefault(x.Id),
                Observacion = observacionesEnviadas?.GetValueOrDefault(x.Id)
            })
            .ToList();
    }

    private async Task<IReadOnlyList<AmbienteCapturaOpcion>> ObtenerAmbientesAutorizadosAsync(
        ContextoCapturaRegistro contexto,
        CancellationToken cancellationToken)
    {
        if (contexto.EsSupervisor)
        {
            return await _context.Ambientes
                .AsNoTracking()
                .Where(x => x.Activo)
                .OrderBy(x => x.Nombre)
                .Select(x => new AmbienteCapturaOpcion(x.Id, x.Nombre, false))
                .ToListAsync(cancellationToken);
        }

        return await _context.UsuariosAmbientes
            .AsNoTracking()
            .Where(x =>
                x.UsuarioId == contexto.UsuarioId &&
                x.Activo &&
                x.Ambiente.Activo)
            .OrderByDescending(x => x.EsPredeterminado)
            .ThenBy(x => x.Ambiente.Nombre)
            .Select(x => new AmbienteCapturaOpcion(
                x.AmbienteId,
                x.Ambiente.Nombre,
                x.EsPredeterminado))
            .ToListAsync(cancellationToken);
    }

    private async Task AsociarRegularizacionAsync(
        Registro registro,
        HorarioCapturaOpcion horario,
        DateTimeOffset ahoraLocal,
        CancellationToken cancellationToken)
    {
        var incidencia = await _context.AlertasRegistrosOmitidos.SingleOrDefaultAsync(x =>
            x.FechaOperativa == horario.FechaOperativa &&
            x.AmbienteId == registro.AmbienteId &&
            x.HorarioId == horario.HorarioId,
            cancellationToken);

        if (incidencia is null)
        {
            incidencia = new AlertaRegistroOmitido
            {
                FechaOperativa = horario.FechaOperativa,
                AmbienteId = registro.AmbienteId,
                HorarioId = horario.HorarioId,
                FechaHoraCierre = horario.Cierre,
                FechaHoraDeteccion = ahoraLocal,
                Estado = EstadoAlertaRegistroOmitido.Pendiente
            };
            _context.AlertasRegistrosOmitidos.Add(incidencia);
        }

        incidencia.EstadoIncidencia = EstadoIncidenciaRegistro.RegularizadaFueraDePlazo;
        incidencia.FechaHoraRegularizacion = ahoraLocal;
        incidencia.RegistroRegularizacion = registro;
        incidencia.Estado = EstadoAlertaRegistroOmitido.Pendiente;
        incidencia.UltimoError = null;
    }

    private static void ValidarMediciones(
        IReadOnlyList<MedicionCapturaInput> mediciones,
        bool confirmacionFueraDeRango,
        ICollection<ErrorCasoUso> errores)
    {
        for (var indice = 0; indice < mediciones.Count; indice++)
        {
            var medicion = mediciones[indice];
            if (medicion.Valor is null)
            {
                errores.Add(new ErrorCasoUso(
                    $"Mediciones[{indice}].Valor",
                    "Ingresa un valor."));
                continue;
            }

            if (decimal.Round(medicion.Valor.Value, medicion.DecimalesPermitidos) != medicion.Valor.Value)
            {
                errores.Add(new ErrorCasoUso(
                    $"Mediciones[{indice}].Valor",
                    $"Usa como máximo {medicion.DecimalesPermitidos} decimal(es)."));
            }

            medicion.Observacion = string.IsNullOrWhiteSpace(medicion.Observacion)
                ? null
                : medicion.Observacion.Trim();
            if (medicion.Observacion?.Length > 500)
            {
                errores.Add(new ErrorCasoUso(
                    $"Mediciones[{indice}].Observacion",
                    "La observación admite hasta 500 caracteres."));
            }
        }

        if (mediciones.Any(x =>
                x.Valor.HasValue &&
                EvaluarRango(x.Valor.Value, x.RangoMinimo, x.RangoMaximo) !=
                    EstadoRango.DentroDeRango) &&
            !confirmacionFueraDeRango)
        {
            errores.Add(new ErrorCasoUso(
                string.Empty,
                "Hay mediciones fuera del rango permitido. Revisa los valores y confirma expresamente el registro."));
        }
    }

    private static void ValidarMotivoFueraDePlazo(
        HorarioCapturaOpcion horario,
        string? motivoFueraDePlazo,
        ICollection<ErrorCasoUso> errores)
    {
        if (horario.Puntualidad != EstadoPuntualidad.FueraDePlazo)
        {
            return;
        }

        if (motivoFueraDePlazo is null)
        {
            errores.Add(new ErrorCasoUso(
                nameof(SolicitudCapturaRegistro.MotivoFueraDePlazo),
                "Explica por qué la medición se está regularizando fuera de plazo."));
        }
        else if (motivoFueraDePlazo.Length > 500)
        {
            errores.Add(new ErrorCasoUso(
                nameof(SolicitudCapturaRegistro.MotivoFueraDePlazo),
                "El motivo admite hasta 500 caracteres."));
        }
    }

    private static EstadoRango EvaluarRango(decimal valor, decimal minimo, decimal maximo)
    {
        if (valor < minimo)
        {
            return EstadoRango.PorDebajo;
        }

        return valor > maximo ? EstadoRango.PorEncima : EstadoRango.DentroDeRango;
    }

    private static PreparacionCapturaRegistro CrearPreparacion(
        bool autorizado,
        SeleccionCapturaRegistro seleccion,
        IReadOnlyList<AmbienteCapturaOpcion> ambientes,
        string? mensajeSinVentana = null)
    {
        return new PreparacionCapturaRegistro(
            autorizado,
            seleccion.AmbienteId,
            seleccion.HorarioId,
            seleccion.FechaOperativa,
            ambientes,
            [],
            [],
            null,
            null,
            mensajeSinVentana);
    }

    private static ResultadoCapturaRegistro CrearResultado(
        PreparacionCapturaRegistro preparacion,
        IReadOnlyList<ErrorCasoUso> errores,
        string? motivoFueraDePlazo)
    {
        return new ResultadoCapturaRegistro(
            preparacion,
            errores,
            motivoFueraDePlazo,
            false,
            null);
    }

    private sealed record RegistroExistente(DateOnly FechaOperativa, int HorarioId);
}
