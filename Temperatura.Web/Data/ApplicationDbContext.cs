using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Domain;

namespace Temperatura.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Ambiente> Ambientes => Set<Ambiente>();

    public DbSet<UsuarioAmbiente> UsuariosAmbientes => Set<UsuarioAmbiente>();

    public DbSet<TipoMedicion> TiposMedicion => Set<TipoMedicion>();

    public DbSet<AmbienteMedicion> AmbientesMediciones => Set<AmbienteMedicion>();

    public DbSet<Horario> Horarios => Set<Horario>();

    public DbSet<AmbienteHorario> AmbientesHorarios => Set<AmbienteHorario>();

    public DbSet<Registro> Registros => Set<Registro>();

    public DbSet<DetalleRegistro> DetallesRegistro => Set<DetalleRegistro>();

    public DbSet<ConfiguracionSmtp> ConfiguracionesSmtp => Set<ConfiguracionSmtp>();

    public DbSet<AlertaRegistroOmitido> AlertasRegistrosOmitidos => Set<AlertaRegistroOmitido>();

    public DbSet<AlertaRegistroFueraRango> AlertasRegistrosFueraRango =>
        Set<AlertaRegistroFueraRango>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
