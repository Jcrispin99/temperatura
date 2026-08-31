using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Data;
using Temperatura.Web.Domain;
using Temperatura.Web.Domain.Enums;

namespace Temperatura.Web.Services;

public sealed class AlertaRegistroFueraRangoService(
    ApplicationDbContext context,
    IVentanaRegistroService ventanaRegistroService,
    ICorreoSmtpSender correoSender,
    ILogger<AlertaRegistroFueraRangoService> logger) : IAlertaRegistroFueraRangoService
{
    private const int MaximoReintentosPorRevision = 25;
    private readonly ApplicationDbContext _context = context;
    private readonly IVentanaRegistroService _ventanaRegistroService = ventanaRegistroService;
    private readonly ICorreoSmtpSender _correoSender = correoSender;
    private readonly ILogger<AlertaRegistroFueraRangoService> _logger = logger;

    public async Task<ResultadoAlertaFueraRango> RegistrarYNotificarAsync(
        long registroId,
        CancellationToken cancellationToken = default)
    {
        var tieneValoresFueraRango = await _context.DetallesRegistro
            .AsNoTracking()
            .AnyAsync(
                x => x.RegistroId == registroId &&
                     x.EstadoRango != EstadoRango.DentroDeRango,
                cancellationToken);

        if (!tieneValoresFueraRango)
        {
            return new ResultadoAlertaFueraRango(false, false);
        }

        var alerta = await _context.AlertasRegistrosFueraRango
            .SingleOrDefaultAsync(x => x.RegistroId == registroId, cancellationToken);
        if (alerta is null)
        {
            alerta = new AlertaRegistroFueraRango
            {
                RegistroId = registroId,
                FechaHoraDeteccion = _ventanaRegistroService.ObtenerAhoraLocal(),
                Estado = EstadoAlertaRango.Pendiente
            };
            _context.AlertasRegistrosFueraRango.Add(alerta);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var correoEnviado = alerta.Estado == EstadoAlertaRango.Enviada ||
                            await EnviarAsync(alerta.Id, cancellationToken);
        return new ResultadoAlertaFueraRango(true, correoEnviado);
    }

    public async Task<int> ReintentarPendientesAsync(
        CancellationToken cancellationToken = default)
    {
        var ids = await _context.AlertasRegistrosFueraRango
            .AsNoTracking()
            .Where(x => x.Estado != EstadoAlertaRango.Enviada)
            .OrderBy(x => x.IntentosEnvio)
            .ThenBy(x => x.FechaHoraDeteccion)
            .Select(x => x.Id)
            .Take(MaximoReintentosPorRevision)
            .ToListAsync(cancellationToken);

        var enviadas = 0;
        foreach (var id in ids)
        {
            if (await EnviarAsync(id, cancellationToken))
            {
                enviadas++;
            }
        }

        return enviadas;
    }

    private async Task<bool> EnviarAsync(long alertaId, CancellationToken cancellationToken)
    {
        var alerta = await _context.AlertasRegistrosFueraRango
            .Include(x => x.Registro)
                .ThenInclude(x => x.Ambiente)
            .Include(x => x.Registro)
                .ThenInclude(x => x.Usuario)
            .Include(x => x.Registro)
                .ThenInclude(x => x.Detalles)
                    .ThenInclude(x => x.TipoMedicion)
            .SingleAsync(x => x.Id == alertaId, cancellationToken);

        if (alerta.Estado == EstadoAlertaRango.Enviada)
        {
            return true;
        }

        var configuracionSmtp = await _context.ConfiguracionesSmtp
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == 1 && x.Activo, cancellationToken);
        var destinatarios = await ObtenerCorreosSupervisoresAsync(cancellationToken);
        if (configuracionSmtp is null || destinatarios.Count == 0)
        {
            _logger.LogWarning(
                "La alerta de rango del registro {RegistroId} está pendiente porque SMTP o los destinatarios no están configurados.",
                alerta.RegistroId);
            return false;
        }

        alerta.IntentosEnvio++;
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            var contenido = CorreoAlertaFueraRango.Crear(alerta);
            await _correoSender.EnviarAsync(
                configuracionSmtp,
                destinatarios,
                contenido.Asunto,
                contenido.CuerpoHtml,
                cancellationToken);

            alerta.Estado = EstadoAlertaRango.Enviada;
            alerta.FechaHoraEnvio = _ventanaRegistroService.ObtenerAhoraLocal();
            alerta.UltimoError = null;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            alerta.Estado = EstadoAlertaRango.Fallida;
            alerta.UltimoError = exception.Message.Length > 1000
                ? exception.Message[..1000]
                : exception.Message;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogError(
                exception,
                "Falló el envío de la alerta de medición fuera de rango del registro {RegistroId}.",
                alerta.RegistroId);
            return false;
        }
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
}
