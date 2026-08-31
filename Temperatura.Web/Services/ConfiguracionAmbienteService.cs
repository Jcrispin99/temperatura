using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Data;
using Temperatura.Web.Domain;
using Temperatura.Web.Domain.Enums;

namespace Temperatura.Web.Services;

public sealed class ConfiguracionAmbienteService(
    ApplicationDbContext context,
    IVentanaRegistroService ventanaRegistroService) : IConfiguracionAmbienteService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IVentanaRegistroService _ventanaRegistroService = ventanaRegistroService;

    public async Task<ResultadoConsultaConfiguracionAmbiente> ObtenerAsync(
        int ambienteId,
        CancellationToken cancellationToken = default)
    {
        var ambiente = await _context.Ambientes
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == ambienteId, cancellationToken);
        if (ambiente is null)
        {
            return new ResultadoConsultaConfiguracionAmbiente(
                false,
                new AmbienteConfiguracionInput { Id = ambienteId },
                []);
        }

        var configuraciones = await _context.AmbientesMediciones
            .AsNoTracking()
            .Where(x => x.AmbienteId == ambienteId && x.Activo)
            .ToDictionaryAsync(x => x.TipoMedicionId, cancellationToken);
        var configuracionesHorario = await _context.AmbientesHorarios
            .AsNoTracking()
            .Where(x => x.AmbienteId == ambienteId && x.Activo)
            .ToDictionaryAsync(x => x.HorarioId, cancellationToken);
        var tipos = await ObtenerTiposMedicionAsync(cancellationToken);
        var horarios = await ObtenerHorariosAsync(cancellationToken);

        var input = new AmbienteConfiguracionInput
        {
            Id = ambiente.Id,
            Nombre = ambiente.Nombre,
            Activo = ambiente.Activo,
            Mediciones = tipos.Select(tipo =>
            {
                configuraciones.TryGetValue(tipo.Id, out var configuracion);
                return CrearMedicionInput(tipo, configuracion);
            }).ToList(),
            Horarios = horarios.Select(horario =>
            {
                configuracionesHorario.TryGetValue(horario.Id, out var configuracion);
                return CrearHorarioInput(horario, configuracion);
            }).ToList()
        };

        return new ResultadoConsultaConfiguracionAmbiente(
            true,
            input,
            await ObtenerHistorialHorariosAsync(ambienteId, cancellationToken));
    }

    public async Task<ResultadoActualizacionAmbiente> ActualizarAsync(
        AmbienteConfiguracionInput input,
        CancellationToken cancellationToken = default)
    {
        var ambiente = await _context.Ambientes
            .SingleOrDefaultAsync(x => x.Id == input.Id, cancellationToken);
        if (ambiente is null)
        {
            return new ResultadoActualizacionAmbiente(
                false,
                false,
                input,
                [],
                [],
                null);
        }

        var errores = new List<ErrorCasoUso>();
        var tipos = await ObtenerTiposMedicionAsync(cancellationToken);
        var horarios = await ObtenerHorariosAsync(cancellationToken);
        NormalizarMedicionesEnviadas(input, tipos, errores);
        NormalizarHorariosEnviados(input, horarios, errores);

        input.Nombre = (input.Nombre ?? string.Empty).Trim();
        if (input.Nombre.Length == 0)
        {
            errores.Add(new ErrorCasoUso("Input.Nombre", "Ingresa el nombre."));
        }
        else if (input.Nombre.Length > 100)
        {
            errores.Add(new ErrorCasoUso(
                "Input.Nombre",
                "El nombre admite hasta 100 caracteres."));
        }
        else if (await _context.Ambientes.AnyAsync(
                     x => x.Id != input.Id && x.Nombre == input.Nombre,
                     cancellationToken))
        {
            errores.Add(new ErrorCasoUso(
                "Input.Nombre",
                "Ya existe un ambiente con este nombre."));
        }

        ValidarMediciones(input.Mediciones, errores);
        ValidarHorarios(input.Horarios, errores);
        if (errores.Count > 0)
        {
            return await CrearResultadoFallidoAsync(ambiente.Id, input, errores, cancellationToken);
        }

        ambiente.Nombre = input.Nombre;
        ambiente.Activo = input.Activo;

        var fechaActual = DateOnly.FromDateTime(_ventanaRegistroService.ObtenerAhoraLocal().DateTime);
        var configuraciones = await _context.AmbientesMediciones
            .Where(x => x.AmbienteId == ambiente.Id)
            .OrderByDescending(x => x.VigenteDesde)
            .ToListAsync(cancellationToken);

        foreach (var medicion in input.Mediciones)
        {
            AplicarConfiguracionMedicion(
                ambiente.Id,
                medicion,
                configuraciones.Where(x => x.TipoMedicionId == medicion.TipoMedicionId).ToList(),
                fechaActual);
        }

        var configuracionesHorario = await _context.AmbientesHorarios
            .Where(x => x.AmbienteId == ambiente.Id)
            .OrderByDescending(x => x.VigenteDesde)
            .ToListAsync(cancellationToken);

        foreach (var horario in input.Horarios)
        {
            AplicarConfiguracionHorario(
                ambiente.Id,
                horario,
                configuracionesHorario.Where(x => x.HorarioId == horario.HorarioId).ToList(),
                fechaActual);
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            errores.Add(new ErrorCasoUso(
                string.Empty,
                "No se pudo guardar la configuración. Recarga la página e inténtalo nuevamente."));
            return await CrearResultadoFallidoAsync(ambiente.Id, input, errores, cancellationToken);
        }

        return new ResultadoActualizacionAmbiente(
            true,
            true,
            input,
            [],
            [],
            ambiente.Nombre);
    }

    private async Task<List<TipoMedicion>> ObtenerTiposMedicionAsync(
        CancellationToken cancellationToken)
    {
        return await _context.TiposMedicion
            .AsNoTracking()
            .Where(x => x.Activo)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<Horario>> ObtenerHorariosAsync(CancellationToken cancellationToken)
    {
        return await _context.Horarios
            .AsNoTracking()
            .Where(x => x.Activo)
            .OrderBy(x => x.MomentoOperativo)
            .ThenBy(x => x.HoraReferencia)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<HorarioAmbienteHistorico>> ObtenerHistorialHorariosAsync(
        int ambienteId,
        CancellationToken cancellationToken)
    {
        return await _context.AmbientesHorarios
            .AsNoTracking()
            .Where(x => x.AmbienteId == ambienteId && !x.Activo)
            .OrderByDescending(x => x.VigenteDesde)
            .ThenBy(x => x.Horario.HoraReferencia)
            .Select(x => new HorarioAmbienteHistorico(
                x.Horario.Nombre,
                x.MinutosAntes,
                x.MinutosToleranciaPuntualidad,
                x.MinutosDespues,
                x.MinutosRegularizacion,
                x.VigenteDesde,
                x.VigenteHasta))
            .ToListAsync(cancellationToken);
    }

    private static void NormalizarMedicionesEnviadas(
        AmbienteConfiguracionInput input,
        IReadOnlyCollection<TipoMedicion> tipos,
        ICollection<ErrorCasoUso> errores)
    {
        var gruposEnviados = input.Mediciones.GroupBy(x => x.TipoMedicionId).ToList();
        if (gruposEnviados.Any(x => x.Count() > 1))
        {
            errores.Add(new ErrorCasoUso(
                string.Empty,
                "La solicitud contiene mediciones duplicadas."));
        }

        var enviadas = gruposEnviados.ToDictionary(x => x.Key, x => x.First());
        var idsValidos = tipos.Select(x => x.Id).ToHashSet();
        if (enviadas.Keys.Any(x => !idsValidos.Contains(x)))
        {
            errores.Add(new ErrorCasoUso(
                string.Empty,
                "La solicitud contiene un tipo de medición inválido."));
        }

        input.Mediciones = tipos.Select(tipo =>
        {
            enviadas.TryGetValue(tipo.Id, out var enviada);
            return new MedicionAmbienteInput
            {
                TipoMedicionId = tipo.Id,
                Nombre = tipo.Nombre,
                Unidad = tipo.SimboloUnidad,
                Habilitada = enviada?.Habilitada ?? false,
                RangoMinimo = enviada?.RangoMinimo,
                RangoMaximo = enviada?.RangoMaximo
            };
        }).ToList();
    }

    private static void NormalizarHorariosEnviados(
        AmbienteConfiguracionInput input,
        IReadOnlyCollection<Horario> horarios,
        ICollection<ErrorCasoUso> errores)
    {
        var gruposEnviados = input.Horarios.GroupBy(x => x.HorarioId).ToList();
        if (gruposEnviados.Any(x => x.Count() > 1))
        {
            errores.Add(new ErrorCasoUso(string.Empty, "La solicitud contiene horarios duplicados."));
        }

        var enviados = gruposEnviados.ToDictionary(x => x.Key, x => x.First());
        var idsValidos = horarios.Select(x => x.Id).ToHashSet();
        if (enviados.Keys.Any(x => !idsValidos.Contains(x)))
        {
            errores.Add(new ErrorCasoUso(string.Empty, "La solicitud contiene un horario inválido."));
        }

        input.Horarios = horarios.Select(horario =>
        {
            enviados.TryGetValue(horario.Id, out var enviado);
            return new HorarioAmbienteInput
            {
                HorarioId = horario.Id,
                Nombre = horario.Nombre,
                MomentoOperativo = horario.MomentoOperativo,
                EsCierreDiaOperativoAnterior = horario.EsCierreDiaOperativoAnterior,
                Habilitado = enviado?.Habilitado ?? false,
                MinutosAntes = enviado?.MinutosAntes,
                MinutosToleranciaPuntualidad = enviado?.MinutosToleranciaPuntualidad,
                MinutosDespues = enviado?.MinutosDespues,
                MinutosRegularizacion = enviado?.MinutosRegularizacion
            };
        }).ToList();
    }

    private static void ValidarMediciones(
        IReadOnlyList<MedicionAmbienteInput> mediciones,
        ICollection<ErrorCasoUso> errores)
    {
        for (var indice = 0; indice < mediciones.Count; indice++)
        {
            var medicion = mediciones[indice];
            if (!medicion.Habilitada)
            {
                continue;
            }

            if (medicion.RangoMinimo is null)
            {
                errores.Add(new ErrorCasoUso(
                    $"Input.Mediciones[{indice}].RangoMinimo",
                    "Ingresa el rango mínimo."));
            }

            if (medicion.RangoMaximo is null)
            {
                errores.Add(new ErrorCasoUso(
                    $"Input.Mediciones[{indice}].RangoMaximo",
                    "Ingresa el rango máximo."));
            }

            if (medicion.RangoMinimo > medicion.RangoMaximo)
            {
                errores.Add(new ErrorCasoUso(
                    $"Input.Mediciones[{indice}].RangoMaximo",
                    "El máximo debe ser mayor o igual que el mínimo."));
            }
        }
    }

    private static void ValidarHorarios(
        IReadOnlyList<HorarioAmbienteInput> horarios,
        ICollection<ErrorCasoUso> errores)
    {
        var momentosDuplicados = horarios
            .Where(x => x.Habilitado)
            .GroupBy(x => x.MomentoOperativo)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToHashSet();

        for (var indice = 0; indice < horarios.Count; indice++)
        {
            var horario = horarios[indice];
            if (!horario.Habilitado)
            {
                continue;
            }

            if (momentosDuplicados.Contains(horario.MomentoOperativo))
            {
                errores.Add(new ErrorCasoUso(
                    $"Input.Horarios[{indice}].Habilitado",
                    $"Selecciona solo un horario para {horario.MomentoOperativo.ObtenerNombre().ToLowerInvariant()}."));
            }

            if (horario.MinutosAntes is null or < 0 or > 720)
            {
                errores.Add(new ErrorCasoUso(
                    $"Input.Horarios[{indice}].MinutosAntes",
                    "Ingresa entre 0 y 720 minutos."));
            }

            if (horario.MinutosDespues is null or < 1 or > 720)
            {
                errores.Add(new ErrorCasoUso(
                    $"Input.Horarios[{indice}].MinutosDespues",
                    "Ingresa entre 1 y 720 minutos."));
            }

            if (horario.MinutosToleranciaPuntualidad is null or < 0 or > 720)
            {
                errores.Add(new ErrorCasoUso(
                    $"Input.Horarios[{indice}].MinutosToleranciaPuntualidad",
                    "Ingresa entre 0 y 720 minutos."));
            }
            else if (horario.MinutosDespues.HasValue &&
                     horario.MinutosToleranciaPuntualidad > horario.MinutosDespues)
            {
                errores.Add(new ErrorCasoUso(
                    $"Input.Horarios[{indice}].MinutosToleranciaPuntualidad",
                    "La tolerancia puntual no puede superar el tiempo posterior de la ventana."));
            }

            if (horario.MinutosRegularizacion is null or < 0 or > 2880)
            {
                errores.Add(new ErrorCasoUso(
                    $"Input.Horarios[{indice}].MinutosRegularizacion",
                    "Ingresa entre 0 y 2880 minutos."));
            }
        }
    }

    private void AplicarConfiguracionMedicion(
        int ambienteId,
        MedicionAmbienteInput medicion,
        IReadOnlyCollection<AmbienteMedicion> configuraciones,
        DateOnly fechaActual)
    {
        var configuracionActiva = configuraciones.SingleOrDefault(x => x.Activo);

        if (!medicion.Habilitada)
        {
            if (configuracionActiva is not null)
            {
                configuracionActiva.Activo = false;
                configuracionActiva.VigenteHasta = configuracionActiva.VigenteDesde < fechaActual
                    ? fechaActual.AddDays(-1)
                    : fechaActual;
            }

            return;
        }

        var minimo = medicion.RangoMinimo!.Value;
        var maximo = medicion.RangoMaximo!.Value;
        if (configuracionActiva is not null &&
            configuracionActiva.RangoMinimo == minimo &&
            configuracionActiva.RangoMaximo == maximo)
        {
            return;
        }

        if (configuracionActiva is not null && configuracionActiva.VigenteDesde >= fechaActual)
        {
            configuracionActiva.RangoMinimo = minimo;
            configuracionActiva.RangoMaximo = maximo;
            configuracionActiva.VigenteHasta = null;
            return;
        }

        if (configuracionActiva is not null)
        {
            configuracionActiva.Activo = false;
            configuracionActiva.VigenteHasta = fechaActual.AddDays(-1);
        }

        var configuracionDelDia = configuraciones.FirstOrDefault(x => x.VigenteDesde == fechaActual);
        if (configuracionDelDia is not null)
        {
            configuracionDelDia.RangoMinimo = minimo;
            configuracionDelDia.RangoMaximo = maximo;
            configuracionDelDia.VigenteHasta = null;
            configuracionDelDia.Activo = true;
            return;
        }

        _context.AmbientesMediciones.Add(new AmbienteMedicion
        {
            AmbienteId = ambienteId,
            TipoMedicionId = medicion.TipoMedicionId,
            RangoMinimo = minimo,
            RangoMaximo = maximo,
            VigenteDesde = fechaActual,
            Activo = true
        });
    }

    private void AplicarConfiguracionHorario(
        int ambienteId,
        HorarioAmbienteInput horario,
        IReadOnlyCollection<AmbienteHorario> configuraciones,
        DateOnly fechaActual)
    {
        var configuracionActiva = configuraciones.SingleOrDefault(x => x.Activo);

        if (!horario.Habilitado)
        {
            if (configuracionActiva is not null)
            {
                configuracionActiva.Activo = false;
                configuracionActiva.VigenteHasta = configuracionActiva.VigenteDesde < fechaActual
                    ? fechaActual.AddDays(-1)
                    : fechaActual;
            }

            return;
        }

        var minutosAntes = horario.MinutosAntes!.Value;
        var minutosToleranciaPuntualidad = horario.MinutosToleranciaPuntualidad!.Value;
        var minutosDespues = horario.MinutosDespues!.Value;
        var minutosRegularizacion = horario.MinutosRegularizacion!.Value;
        if (configuracionActiva is not null &&
            configuracionActiva.MinutosAntes == minutosAntes &&
            configuracionActiva.MinutosToleranciaPuntualidad == minutosToleranciaPuntualidad &&
            configuracionActiva.MinutosDespues == minutosDespues &&
            configuracionActiva.MinutosRegularizacion == minutosRegularizacion)
        {
            return;
        }

        if (configuracionActiva is not null && configuracionActiva.VigenteDesde >= fechaActual)
        {
            configuracionActiva.MinutosAntes = minutosAntes;
            configuracionActiva.MinutosToleranciaPuntualidad = minutosToleranciaPuntualidad;
            configuracionActiva.MinutosDespues = minutosDespues;
            configuracionActiva.MinutosRegularizacion = minutosRegularizacion;
            configuracionActiva.VigenteHasta = null;
            return;
        }

        if (configuracionActiva is not null)
        {
            configuracionActiva.Activo = false;
            configuracionActiva.VigenteHasta = fechaActual.AddDays(-1);
        }

        var configuracionDelDia = configuraciones.FirstOrDefault(x => x.VigenteDesde == fechaActual);
        if (configuracionDelDia is not null)
        {
            configuracionDelDia.MinutosAntes = minutosAntes;
            configuracionDelDia.MinutosToleranciaPuntualidad = minutosToleranciaPuntualidad;
            configuracionDelDia.MinutosDespues = minutosDespues;
            configuracionDelDia.MinutosRegularizacion = minutosRegularizacion;
            configuracionDelDia.VigenteHasta = null;
            configuracionDelDia.Activo = true;
            return;
        }

        _context.AmbientesHorarios.Add(new AmbienteHorario
        {
            AmbienteId = ambienteId,
            HorarioId = horario.HorarioId,
            MinutosAntes = minutosAntes,
            MinutosToleranciaPuntualidad = minutosToleranciaPuntualidad,
            MinutosDespues = minutosDespues,
            MinutosRegularizacion = minutosRegularizacion,
            VigenteDesde = fechaActual,
            Activo = true
        });
    }

    private static MedicionAmbienteInput CrearMedicionInput(
        TipoMedicion tipo,
        AmbienteMedicion? configuracion)
    {
        return new MedicionAmbienteInput
        {
            TipoMedicionId = tipo.Id,
            Nombre = tipo.Nombre,
            Unidad = tipo.SimboloUnidad,
            Habilitada = configuracion is not null,
            RangoMinimo = configuracion?.RangoMinimo,
            RangoMaximo = configuracion?.RangoMaximo
        };
    }

    private static HorarioAmbienteInput CrearHorarioInput(
        Horario horario,
        AmbienteHorario? configuracion)
    {
        return new HorarioAmbienteInput
        {
            HorarioId = horario.Id,
            Nombre = horario.Nombre,
            MomentoOperativo = horario.MomentoOperativo,
            EsCierreDiaOperativoAnterior = horario.EsCierreDiaOperativoAnterior,
            Habilitado = configuracion is not null,
            MinutosAntes = configuracion?.MinutosAntes ?? AmbienteHorario.MinutosAntesPredeterminados,
            MinutosToleranciaPuntualidad = configuracion?.MinutosToleranciaPuntualidad ??
                AmbienteHorario.MinutosToleranciaPuntualidadPredeterminados,
            MinutosDespues = configuracion?.MinutosDespues ??
                AmbienteHorario.MinutosDespuesPredeterminados,
            MinutosRegularizacion = configuracion?.MinutosRegularizacion ??
                AmbienteHorario.MinutosRegularizacionPredeterminados
        };
    }

    private async Task<ResultadoActualizacionAmbiente> CrearResultadoFallidoAsync(
        int ambienteId,
        AmbienteConfiguracionInput input,
        IReadOnlyList<ErrorCasoUso> errores,
        CancellationToken cancellationToken)
    {
        return new ResultadoActualizacionAmbiente(
            true,
            false,
            input,
            await ObtenerHistorialHorariosAsync(ambienteId, cancellationToken),
            errores,
            null);
    }
}
