using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Data;
using Temperatura.Web.Domain;
using Temperatura.Web.Services;

namespace Temperatura.Web.Pages.Admin.Ambientes;

[Authorize(Roles = "Supervisor")]
public class EditarModel(
    ApplicationDbContext context,
    IVentanaRegistroService ventanaRegistroService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly IVentanaRegistroService _ventanaRegistroService = ventanaRegistroService;

    [BindProperty]
    public AmbienteInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var ambiente = await _context.Ambientes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (ambiente is null)
        {
            return NotFound();
        }

        var configuraciones = await _context.AmbientesMediciones
            .AsNoTracking()
            .Where(x => x.AmbienteId == id && x.Activo)
            .ToDictionaryAsync(x => x.TipoMedicionId);

        var configuracionesHorario = await _context.AmbientesHorarios
            .AsNoTracking()
            .Where(x => x.AmbienteId == id && x.Activo)
            .ToDictionaryAsync(x => x.HorarioId);

        var tipos = await ObtenerTiposMedicionAsync();
        var horarios = await ObtenerHorariosAsync();
        Input = new AmbienteInput
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

        HistorialHorarios = await ObtenerHistorialHorariosAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var ambiente = await _context.Ambientes.SingleOrDefaultAsync(x => x.Id == Input.Id);
        if (ambiente is null)
        {
            return NotFound();
        }

        var tipos = await ObtenerTiposMedicionAsync();
        var horarios = await ObtenerHorariosAsync();
        NormalizarMedicionesEnviadas(tipos);
        NormalizarHorariosEnviados(horarios);

        Input.Nombre = Input.Nombre.Trim();
        if (await _context.Ambientes.AnyAsync(x => x.Id != Input.Id && x.Nombre == Input.Nombre))
        {
            ModelState.AddModelError("Input.Nombre", "Ya existe un ambiente con este nombre.");
        }

        ValidarMediciones();
        ValidarHorarios();
        if (!ModelState.IsValid)
        {
            HistorialHorarios = await ObtenerHistorialHorariosAsync(ambiente.Id);
            return Page();
        }

        ambiente.Nombre = Input.Nombre;
        ambiente.Activo = Input.Activo;

        var fechaActual = DateOnly.FromDateTime(_ventanaRegistroService.ObtenerAhoraLocal().DateTime);
        var configuraciones = await _context.AmbientesMediciones
            .Where(x => x.AmbienteId == ambiente.Id)
            .OrderByDescending(x => x.VigenteDesde)
            .ToListAsync();

        foreach (var medicion in Input.Mediciones)
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
            .ToListAsync();

        foreach (var horario in Input.Horarios)
        {
            AplicarConfiguracionHorario(
                ambiente.Id,
                horario,
                configuracionesHorario.Where(x => x.HorarioId == horario.HorarioId).ToList(),
                fechaActual);
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "No se pudo guardar la configuración. Recarga la página e inténtalo nuevamente.");
            HistorialHorarios = await ObtenerHistorialHorariosAsync(ambiente.Id);
            return Page();
        }

        TempData["MensajeExito"] = $"El ambiente {ambiente.Nombre}, sus mediciones y horarios fueron actualizados.";
        return RedirectToPage("Index");
    }

    public IReadOnlyList<HorarioHistorico> HistorialHorarios { get; private set; } = [];

    private async Task<List<TipoMedicion>> ObtenerTiposMedicionAsync()
    {
        return await _context.TiposMedicion
            .AsNoTracking()
            .Where(x => x.Activo)
            .OrderBy(x => x.Id)
            .ToListAsync();
    }

    private async Task<List<Horario>> ObtenerHorariosAsync()
    {
        return await _context.Horarios
            .AsNoTracking()
            .Where(x => x.Activo)
            .OrderBy(x => x.EsCierreDiaOperativoAnterior)
            .ThenBy(x => x.HoraReferencia)
            .ToListAsync();
    }

    private async Task<IReadOnlyList<HorarioHistorico>> ObtenerHistorialHorariosAsync(int ambienteId)
    {
        return await _context.AmbientesHorarios
            .AsNoTracking()
            .Where(x => x.AmbienteId == ambienteId && !x.Activo)
            .OrderByDescending(x => x.VigenteDesde)
            .ThenBy(x => x.Horario.HoraReferencia)
            .Select(x => new HorarioHistorico(
                x.Horario.Nombre,
                x.MinutosAntes,
                x.MinutosDespues,
                x.VigenteDesde,
                x.VigenteHasta))
            .ToListAsync();
    }

    private void NormalizarMedicionesEnviadas(IReadOnlyCollection<TipoMedicion> tipos)
    {
        var gruposEnviados = Input.Mediciones.GroupBy(x => x.TipoMedicionId).ToList();
        if (gruposEnviados.Any(x => x.Count() > 1))
        {
            ModelState.AddModelError(string.Empty, "La solicitud contiene mediciones duplicadas.");
        }

        var enviadas = gruposEnviados.ToDictionary(x => x.Key, x => x.First());
        var idsValidos = tipos.Select(x => x.Id).ToHashSet();
        if (enviadas.Keys.Any(x => !idsValidos.Contains(x)))
        {
            ModelState.AddModelError(string.Empty, "La solicitud contiene un tipo de medición inválido.");
        }

        Input.Mediciones = tipos.Select(tipo =>
        {
            enviadas.TryGetValue(tipo.Id, out var enviada);
            return new MedicionInput
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

    private void NormalizarHorariosEnviados(IReadOnlyCollection<Horario> horarios)
    {
        var gruposEnviados = Input.Horarios.GroupBy(x => x.HorarioId).ToList();
        if (gruposEnviados.Any(x => x.Count() > 1))
        {
            ModelState.AddModelError(string.Empty, "La solicitud contiene horarios duplicados.");
        }

        var enviados = gruposEnviados.ToDictionary(x => x.Key, x => x.First());
        var idsValidos = horarios.Select(x => x.Id).ToHashSet();
        if (enviados.Keys.Any(x => !idsValidos.Contains(x)))
        {
            ModelState.AddModelError(string.Empty, "La solicitud contiene un horario inválido.");
        }

        Input.Horarios = horarios.Select(horario =>
        {
            enviados.TryGetValue(horario.Id, out var enviado);
            return new HorarioInput
            {
                HorarioId = horario.Id,
                Nombre = horario.Nombre,
                EsCierreDiaOperativoAnterior = horario.EsCierreDiaOperativoAnterior,
                Habilitado = enviado?.Habilitado ?? false,
                MinutosAntes = enviado?.MinutosAntes,
                MinutosDespues = enviado?.MinutosDespues
            };
        }).ToList();
    }

    private void ValidarMediciones()
    {
        for (var indice = 0; indice < Input.Mediciones.Count; indice++)
        {
            var medicion = Input.Mediciones[indice];
            if (!medicion.Habilitada)
            {
                continue;
            }

            if (medicion.RangoMinimo is null)
            {
                ModelState.AddModelError(
                    $"Input.Mediciones[{indice}].RangoMinimo",
                    "Ingresa el rango mínimo.");
            }

            if (medicion.RangoMaximo is null)
            {
                ModelState.AddModelError(
                    $"Input.Mediciones[{indice}].RangoMaximo",
                    "Ingresa el rango máximo.");
            }

            if (medicion.RangoMinimo > medicion.RangoMaximo)
            {
                ModelState.AddModelError(
                    $"Input.Mediciones[{indice}].RangoMaximo",
                    "El máximo debe ser mayor o igual que el mínimo.");
            }
        }
    }

    private void ValidarHorarios()
    {
        for (var indice = 0; indice < Input.Horarios.Count; indice++)
        {
            var horario = Input.Horarios[indice];
            if (!horario.Habilitado)
            {
                continue;
            }

            if (horario.MinutosAntes is null or < 0 or > 720)
            {
                ModelState.AddModelError(
                    $"Input.Horarios[{indice}].MinutosAntes",
                    "Ingresa entre 0 y 720 minutos.");
            }

            if (horario.MinutosDespues is null or < 1 or > 720)
            {
                ModelState.AddModelError(
                    $"Input.Horarios[{indice}].MinutosDespues",
                    "Ingresa entre 1 y 720 minutos.");
            }
        }
    }

    private void AplicarConfiguracionMedicion(
        int ambienteId,
        MedicionInput medicion,
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

    private static MedicionInput CrearMedicionInput(
        TipoMedicion tipo,
        AmbienteMedicion? configuracion)
    {
        return new MedicionInput
        {
            TipoMedicionId = tipo.Id,
            Nombre = tipo.Nombre,
            Unidad = tipo.SimboloUnidad,
            Habilitada = configuracion is not null,
            RangoMinimo = configuracion?.RangoMinimo,
            RangoMaximo = configuracion?.RangoMaximo
        };
    }

    private void AplicarConfiguracionHorario(
        int ambienteId,
        HorarioInput horario,
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
        var minutosDespues = horario.MinutosDespues!.Value;
        if (configuracionActiva is not null &&
            configuracionActiva.MinutosAntes == minutosAntes &&
            configuracionActiva.MinutosDespues == minutosDespues)
        {
            return;
        }

        if (configuracionActiva is not null && configuracionActiva.VigenteDesde >= fechaActual)
        {
            configuracionActiva.MinutosAntes = minutosAntes;
            configuracionActiva.MinutosDespues = minutosDespues;
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
            configuracionDelDia.MinutosDespues = minutosDespues;
            configuracionDelDia.VigenteHasta = null;
            configuracionDelDia.Activo = true;
            return;
        }

        _context.AmbientesHorarios.Add(new AmbienteHorario
        {
            AmbienteId = ambienteId,
            HorarioId = horario.HorarioId,
            MinutosAntes = minutosAntes,
            MinutosDespues = minutosDespues,
            VigenteDesde = fechaActual,
            Activo = true
        });
    }

    private static HorarioInput CrearHorarioInput(Horario horario, AmbienteHorario? configuracion)
    {
        return new HorarioInput
        {
            HorarioId = horario.Id,
            Nombre = horario.Nombre,
            EsCierreDiaOperativoAnterior = horario.EsCierreDiaOperativoAnterior,
            Habilitado = configuracion is not null,
            MinutosAntes = configuracion?.MinutosAntes ?? 30,
            MinutosDespues = configuracion?.MinutosDespues ?? 60
        };
    }

    public sealed class AmbienteInput
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Ingresa el nombre.")]
        [StringLength(100, ErrorMessage = "El nombre admite hasta 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        public bool Activo { get; set; }

        public List<MedicionInput> Mediciones { get; set; } = [];

        public List<HorarioInput> Horarios { get; set; } = [];
    }

    public sealed class MedicionInput
    {
        public int TipoMedicionId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Unidad { get; set; } = string.Empty;

        public bool Habilitada { get; set; }

        public decimal? RangoMinimo { get; set; }

        public decimal? RangoMaximo { get; set; }
    }

    public sealed class HorarioInput
    {
        public int HorarioId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public bool EsCierreDiaOperativoAnterior { get; set; }

        public bool Habilitado { get; set; }

        public short? MinutosAntes { get; set; }

        public short? MinutosDespues { get; set; }
    }

    public sealed record HorarioHistorico(
        string Horario,
        short MinutosAntes,
        short MinutosDespues,
        DateOnly VigenteDesde,
        DateOnly? VigenteHasta);
}
