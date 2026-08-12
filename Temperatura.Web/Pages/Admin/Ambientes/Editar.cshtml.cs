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

        var tipos = await ObtenerTiposMedicionAsync();
        Input = new AmbienteInput
        {
            Id = ambiente.Id,
            Nombre = ambiente.Nombre,
            Activo = ambiente.Activo,
            Mediciones = tipos.Select(tipo =>
            {
                configuraciones.TryGetValue(tipo.Id, out var configuracion);
                return CrearMedicionInput(tipo, configuracion);
            }).ToList()
        };
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
        NormalizarMedicionesEnviadas(tipos);

        Input.Nombre = Input.Nombre.Trim();
        if (await _context.Ambientes.AnyAsync(x => x.Id != Input.Id && x.Nombre == Input.Nombre))
        {
            ModelState.AddModelError("Input.Nombre", "Ya existe un ambiente con este nombre.");
        }

        ValidarMediciones();
        if (!ModelState.IsValid)
        {
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

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "No se pudo guardar la configuración. Recarga la página e inténtalo nuevamente.");
            return Page();
        }

        TempData["MensajeExito"] = $"El ambiente {ambiente.Nombre} y sus mediciones fueron actualizados.";
        return RedirectToPage("Index");
    }

    private async Task<List<TipoMedicion>> ObtenerTiposMedicionAsync()
    {
        return await _context.TiposMedicion
            .AsNoTracking()
            .Where(x => x.Activo)
            .OrderBy(x => x.Id)
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

    public sealed class AmbienteInput
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Ingresa el nombre.")]
        [StringLength(100, ErrorMessage = "El nombre admite hasta 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        public bool Activo { get; set; }

        public List<MedicionInput> Mediciones { get; set; } = [];
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
}
