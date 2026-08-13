using System.Net;
using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Data;
using Temperatura.Web.Domain;
using Temperatura.Web.Domain.Enums;

namespace Temperatura.Web.Services;

public sealed class AlertaRegistroOmitidoService(
    ApplicationDbContext context,
    IVentanaRegistroService ventanaRegistroService,
    ICorreoSmtpSender correoSender,
    IConfiguration configuration,
    ILogger<AlertaRegistroOmitidoService> logger) : IAlertaRegistroOmitidoService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IVentanaRegistroService _ventanaRegistroService = ventanaRegistroService;
    private readonly ICorreoSmtpSender _correoSender = correoSender;
    private readonly ILogger<AlertaRegistroOmitidoService> _logger = logger;
    private readonly TimeZoneInfo _zonaHoraria = TimeZoneInfo.FindSystemTimeZoneById(
        configuration["Sistema:ZonaHoraria"] ?? "America/Lima");

    public async Task<ResultadoRevisionOmisiones> RevisarYNotificarAsync(
        CancellationToken cancellationToken = default)
    {
        var ahora = _ventanaRegistroService.ObtenerAhoraLocal();
        var hoy = DateOnly.FromDateTime(ahora.DateTime);
        var fechas = new[] { hoy.AddDays(-1), hoy };

        var configuraciones = await _context.AmbientesHorarios
            .AsNoTracking()
            .Include(x => x.Ambiente)
            .Include(x => x.Horario)
            .Where(x =>
                x.Ambiente.Activo &&
                x.Horario.Activo &&
                x.VigenteDesde <= hoy &&
                (x.VigenteHasta == null || x.VigenteHasta >= fechas[0]))
            .ToListAsync(cancellationToken);

        var esperados = fechas.SelectMany(fecha => configuraciones
                .Where(x => x.VigenteDesde <= fecha &&
                            (x.VigenteHasta == null || x.VigenteHasta >= fecha))
                .GroupBy(x => new { x.AmbienteId, x.HorarioId })
                .Select(x => x
                    .OrderByDescending(y => y.Activo)
                    .ThenByDescending(y => y.VigenteDesde)
                    .ThenByDescending(y => y.Id)
                    .First())
                .Where(x => x.Activo)
                .Select(x => new RegistroEsperado(
                    fecha,
                    x.AmbienteId,
                    x.Ambiente.Nombre,
                    x.HorarioId,
                    x.Horario.Nombre,
                    CrearCierre(fecha, x))))
            .ToArray();

        var realizados = (await _context.Registros
                .AsNoTracking()
                .Where(x => fechas.Contains(x.FechaOperativa) &&
                            x.Estado == EstadoRegistro.Confirmado)
                .Select(x => new { x.FechaOperativa, x.AmbienteId, x.HorarioId })
                .ToListAsync(cancellationToken))
            .Select(x => new RegistroRealizado(x.FechaOperativa, x.AmbienteId, x.HorarioId))
            .ToHashSet();

        var alertados = (await _context.AlertasRegistrosOmitidos
                .AsNoTracking()
                .Where(x => fechas.Contains(x.FechaOperativa))
                .Select(x => new { x.FechaOperativa, x.AmbienteId, x.HorarioId })
                .ToListAsync(cancellationToken))
            .Select(x => new RegistroRealizado(x.FechaOperativa, x.AmbienteId, x.HorarioId))
            .ToHashSet();

        var omisiones = DetectorRegistrosOmitidos.Detectar(esperados, realizados, alertados, ahora);
        if (omisiones.Count > 0)
        {
            _context.AlertasRegistrosOmitidos.AddRange(omisiones.Select(x => new AlertaRegistroOmitido
            {
                FechaOperativa = x.FechaOperativa,
                AmbienteId = x.AmbienteId,
                HorarioId = x.HorarioId,
                FechaHoraCierre = x.FechaHoraCierre,
                FechaHoraDeteccion = ahora,
                Estado = EstadoAlertaRegistroOmitido.Pendiente
            }));
            await _context.SaveChangesAsync(cancellationToken);
        }

        var pendientes = await _context.AlertasRegistrosOmitidos
            .Include(x => x.Ambiente)
            .Include(x => x.Horario)
            .Where(x => fechas.Contains(x.FechaOperativa) &&
                        x.Estado != EstadoAlertaRegistroOmitido.Enviada)
            .OrderBy(x => x.FechaHoraCierre)
            .ToListAsync(cancellationToken);

        if (pendientes.Count == 0)
        {
            return new ResultadoRevisionOmisiones(omisiones.Count, 0, false);
        }

        var configuracionSmtp = await _context.ConfiguracionesSmtp
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == 1 && x.Activo, cancellationToken);
        var destinatarios = await ObtenerCorreosSupervisoresAsync(cancellationToken);
        if (configuracionSmtp is null || destinatarios.Count == 0)
        {
            _logger.LogWarning(
                "Hay {Cantidad} alerta(s) de registro omitido pendientes, pero SMTP o los destinatarios no están configurados.",
                pendientes.Count);
            return new ResultadoRevisionOmisiones(omisiones.Count, 0, false);
        }

        foreach (var alerta in pendientes)
        {
            alerta.IntentosEnvio++;
        }

        try
        {
            await _correoSender.EnviarAsync(
                configuracionSmtp,
                destinatarios,
                $"Registros ambientales omitidos ({pendientes.Count})",
                CrearCuerpoCorreo(pendientes, ahora),
                cancellationToken);

            foreach (var alerta in pendientes)
            {
                alerta.Estado = EstadoAlertaRegistroOmitido.Enviada;
                alerta.FechaHoraEnvio = ahora;
                alerta.UltimoError = null;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return new ResultadoRevisionOmisiones(omisiones.Count, pendientes.Count, true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var error = exception.Message.Length > 1000
                ? exception.Message[..1000]
                : exception.Message;
            foreach (var alerta in pendientes)
            {
                alerta.Estado = EstadoAlertaRegistroOmitido.Fallida;
                alerta.UltimoError = error;
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogError(exception, "Falló el envío de {Cantidad} alerta(s) por SMTP.", pendientes.Count);
            return new ResultadoRevisionOmisiones(omisiones.Count, 0, false);
        }
    }

    private DateTimeOffset CrearCierre(DateOnly fechaOperativa, AmbienteHorario configuracion)
    {
        var fechaCalendario = configuracion.Horario.EsCierreDiaOperativoAnterior
            ? fechaOperativa.AddDays(1)
            : fechaOperativa;
        var fechaHora = DateTime.SpecifyKind(
            fechaCalendario.ToDateTime(configuracion.Horario.HoraReferencia),
            DateTimeKind.Unspecified);
        return new DateTimeOffset(fechaHora, _zonaHoraria.GetUtcOffset(fechaHora))
            .AddMinutes(configuracion.MinutosDespues);
    }

    private async Task<IReadOnlyList<string>> ObtenerCorreosSupervisoresAsync(
        CancellationToken cancellationToken)
    {
        return await (
                from usuarioRol in _context.UserRoles
                join rol in _context.Roles on usuarioRol.RoleId equals rol.Id
                join usuario in _context.Users on usuarioRol.UserId equals usuario.Id
                where rol.NormalizedName == "SUPERVISOR" &&
                      usuario.Activo &&
                      usuario.Email != null
                select usuario.Email)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private static string CrearCuerpoCorreo(
        IReadOnlyCollection<AlertaRegistroOmitido> alertas,
        DateTimeOffset ahora)
    {
        var filas = string.Join(string.Empty, alertas.Select(x =>
            $"<tr><td>{WebUtility.HtmlEncode(x.Ambiente.Nombre)}</td>" +
            $"<td>{WebUtility.HtmlEncode(x.Horario.Nombre)}</td>" +
            $"<td>{x.FechaOperativa:dd/MM/yyyy}</td>" +
            $"<td>{x.FechaHoraCierre:dd/MM/yyyy HH:mm}</td></tr>"));

        return $"""
            <h2>Registros ambientales omitidos</h2>
            <p>Los siguientes ambientes no registraron dentro de la hora establecida:</p>
            <table style="border-collapse:collapse" border="1" cellpadding="8">
              <thead><tr><th>Ambiente</th><th>Horario</th><th>Fecha operativa</th><th>Cierre</th></tr></thead>
              <tbody>{filas}</tbody>
            </table>
            <p>Revisión realizada: {ahora:dd/MM/yyyy HH:mm} (hora local).</p>
            """;
    }
}
