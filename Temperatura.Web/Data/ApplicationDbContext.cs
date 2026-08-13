using Microsoft.AspNetCore.Identity;
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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigurarUsuarios(builder);
        ConfigurarAmbientes(builder);
        ConfigurarMediciones(builder);
        ConfigurarHorarios(builder);
        ConfigurarRegistros(builder);
        ConfigurarSmtp(builder);
        ConfigurarAlertasRegistrosOmitidos(builder);
        CargarDatosIniciales(builder);
    }

    private static void ConfigurarUsuarios(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Activo).HasDefaultValue(true);
        });

        builder.Entity<UsuarioAmbiente>(entity =>
        {
            entity.ToTable("UsuariosAmbientes", table =>
                table.HasCheckConstraint(
                    "CK_UsuariosAmbientes_PredeterminadoActivo",
                    "[EsPredeterminado] = 0 OR [Activo] = 1"));

            entity.HasKey(x => new { x.UsuarioId, x.AmbienteId });
            entity.HasIndex(x => x.UsuarioId)
                .IsUnique()
                .HasFilter("[EsPredeterminado] = 1 AND [Activo] = 1");

            entity.HasOne(x => x.Usuario)
                .WithMany(x => x.Ambientes)
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Ambiente)
                .WithMany(x => x.Usuarios)
                .HasForeignKey(x => x.AmbienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<IdentityRole>().HasData(
            new IdentityRole
            {
                Id = "a83bce0d-b0dc-4a0f-bb38-2d58fce2c001",
                Name = "Registrador",
                NormalizedName = "REGISTRADOR",
                ConcurrencyStamp = "a83bce0d-b0dc-4a0f-bb38-2d58fce2c001"
            },
            new IdentityRole
            {
                Id = "a83bce0d-b0dc-4a0f-bb38-2d58fce2c002",
                Name = "Supervisor",
                NormalizedName = "SUPERVISOR",
                ConcurrencyStamp = "a83bce0d-b0dc-4a0f-bb38-2d58fce2c002"
            });
    }

    private static void ConfigurarAmbientes(ModelBuilder builder)
    {
        builder.Entity<Ambiente>(entity =>
        {
            entity.ToTable("Ambientes");
            entity.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.Nombre).IsUnique();
        });
    }

    private static void ConfigurarMediciones(ModelBuilder builder)
    {
        builder.Entity<TipoMedicion>(entity =>
        {
            entity.ToTable("TiposMedicion", table =>
                table.HasCheckConstraint(
                    "CK_TiposMedicion_Decimales",
                    "[DecimalesPermitidos] BETWEEN 0 AND 4"));

            entity.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(x => x.SimboloUnidad).HasMaxLength(10).IsRequired();
            entity.HasIndex(x => x.Nombre).IsUnique();
        });

        builder.Entity<AmbienteMedicion>(entity =>
        {
            entity.ToTable("AmbientesMediciones", table =>
            {
                table.HasCheckConstraint(
                    "CK_AmbientesMediciones_Rango",
                    "[RangoMinimo] <= [RangoMaximo]");
                table.HasCheckConstraint(
                    "CK_AmbientesMediciones_Vigencia",
                    "[VigenteHasta] IS NULL OR [VigenteHasta] >= [VigenteDesde]");
            });

            entity.Property(x => x.RangoMinimo).HasPrecision(9, 2);
            entity.Property(x => x.RangoMaximo).HasPrecision(9, 2);
            entity.HasIndex(x => new { x.AmbienteId, x.TipoMedicionId, x.VigenteDesde }).IsUnique();
            entity.HasIndex(x => new { x.AmbienteId, x.TipoMedicionId })
                .IsUnique()
                .HasFilter("[Activo] = 1");

            entity.HasOne(x => x.Ambiente)
                .WithMany(x => x.Mediciones)
                .HasForeignKey(x => x.AmbienteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.TipoMedicion)
                .WithMany(x => x.Ambientes)
                .HasForeignKey(x => x.TipoMedicionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurarHorarios(ModelBuilder builder)
    {
        builder.Entity<Horario>(entity =>
        {
            entity.ToTable("Horarios");
            entity.Property(x => x.Nombre).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.HoraReferencia).IsUnique();
        });

        builder.Entity<AmbienteHorario>(entity =>
        {
            entity.ToTable("AmbientesHorarios", table =>
            {
                table.HasCheckConstraint(
                    "CK_AmbientesHorarios_Ventana",
                    "[MinutosAntes] >= 0 AND [MinutosDespues] > 0");
                table.HasCheckConstraint(
                    "CK_AmbientesHorarios_Vigencia",
                    "[VigenteHasta] IS NULL OR [VigenteHasta] >= [VigenteDesde]");
            });

            entity.HasIndex(x => new { x.AmbienteId, x.HorarioId, x.VigenteDesde }).IsUnique();
            entity.HasIndex(x => new { x.AmbienteId, x.HorarioId })
                .IsUnique()
                .HasFilter("[Activo] = 1");

            entity.HasOne(x => x.Ambiente)
                .WithMany(x => x.Horarios)
                .HasForeignKey(x => x.AmbienteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Horario)
                .WithMany(x => x.Ambientes)
                .HasForeignKey(x => x.HorarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurarRegistros(ModelBuilder builder)
    {
        builder.Entity<Registro>(entity =>
        {
            entity.ToTable("Registros");
            entity.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Puntualidad).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => new { x.AmbienteId, x.HorarioId, x.FechaOperativa }).IsUnique();
            entity.HasIndex(x => new { x.FechaOperativa, x.AmbienteId });

            entity.HasOne(x => x.Ambiente)
                .WithMany(x => x.Registros)
                .HasForeignKey(x => x.AmbienteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Horario)
                .WithMany(x => x.Registros)
                .HasForeignKey(x => x.HorarioId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Usuario)
                .WithMany(x => x.Registros)
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<DetalleRegistro>(entity =>
        {
            entity.ToTable("DetallesRegistro", table =>
                table.HasCheckConstraint(
                    "CK_DetallesRegistro_Limites",
                    "[LimiteMinimoAplicado] <= [LimiteMaximoAplicado]"));

            entity.Property(x => x.Valor).HasPrecision(9, 2);
            entity.Property(x => x.LimiteMinimoAplicado).HasPrecision(9, 2);
            entity.Property(x => x.LimiteMaximoAplicado).HasPrecision(9, 2);
            entity.Property(x => x.EstadoRango).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => new { x.RegistroId, x.TipoMedicionId }).IsUnique();

            entity.HasOne(x => x.Registro)
                .WithMany(x => x.Detalles)
                .HasForeignKey(x => x.RegistroId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.AmbienteMedicion)
                .WithMany(x => x.Detalles)
                .HasForeignKey(x => x.AmbienteMedicionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.TipoMedicion)
                .WithMany(x => x.Detalles)
                .HasForeignKey(x => x.TipoMedicionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurarSmtp(ModelBuilder builder)
    {
        builder.Entity<ConfiguracionSmtp>(entity =>
        {
            entity.ToTable("ConfiguracionesSmtp", table =>
            {
                table.HasCheckConstraint(
                    "CK_ConfiguracionesSmtp_RegistroUnico",
                    "[Id] = 1");
                table.HasCheckConstraint(
                    "CK_ConfiguracionesSmtp_Puerto",
                    "[Puerto] BETWEEN 1 AND 65535");
            });

            entity.Property(x => x.Servidor).HasMaxLength(255).IsRequired();
            entity.Property(x => x.CorreoRemitente).HasMaxLength(256).IsRequired();
            entity.Property(x => x.NombreRemitente).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Usuario).HasMaxLength(256).IsRequired();
            entity.Property(x => x.SecretoProtegido).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.ActualizadoPorUsuarioId).HasMaxLength(450).IsRequired();
        });
    }

    private static void ConfigurarAlertasRegistrosOmitidos(ModelBuilder builder)
    {
        builder.Entity<AlertaRegistroOmitido>(entity =>
        {
            entity.ToTable("AlertasRegistrosOmitidos");
            entity.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.UltimoError).HasMaxLength(1000);
            entity.HasIndex(x => new { x.FechaOperativa, x.AmbienteId, x.HorarioId }).IsUnique();
            entity.HasIndex(x => x.Estado);

            entity.HasOne(x => x.Ambiente)
                .WithMany()
                .HasForeignKey(x => x.AmbienteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Horario)
                .WithMany()
                .HasForeignKey(x => x.HorarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void CargarDatosIniciales(ModelBuilder builder)
    {
        var vigenteDesde = new DateOnly(2026, 1, 1);

        builder.Entity<Ambiente>().HasData(
            new Ambiente { Id = 1, Nombre = "Farmacia", Activo = true },
            new Ambiente { Id = 2, Nombre = "Enfermería", Activo = true },
            new Ambiente { Id = 3, Nombre = "UMA 1", Activo = true },
            new Ambiente { Id = 4, Nombre = "UMA 2", Activo = true },
            new Ambiente { Id = 5, Nombre = "UMA 3", Activo = true });

        builder.Entity<TipoMedicion>().HasData(
            new TipoMedicion
            {
                Id = 1,
                Nombre = "Temperatura ambiental",
                SimboloUnidad = "°C",
                DecimalesPermitidos = 1,
                Activo = true
            },
            new TipoMedicion
            {
                Id = 2,
                Nombre = "Humedad relativa",
                SimboloUnidad = "%",
                DecimalesPermitidos = 1,
                Activo = true
            },
            new TipoMedicion
            {
                Id = 3,
                Nombre = "Temperatura de refrigeración",
                SimboloUnidad = "°C",
                DecimalesPermitidos = 1,
                Activo = true
            });

        builder.Entity<Horario>().HasData(
            new Horario
            {
                Id = 1,
                Nombre = "07:00",
                HoraReferencia = new TimeOnly(7, 0),
                EsCierreDiaOperativoAnterior = false,
                Activo = true
            },
            new Horario
            {
                Id = 2,
                Nombre = "12:00",
                HoraReferencia = new TimeOnly(12, 0),
                EsCierreDiaOperativoAnterior = false,
                Activo = true
            },
            new Horario
            {
                Id = 3,
                Nombre = "19:00",
                HoraReferencia = new TimeOnly(19, 0),
                EsCierreDiaOperativoAnterior = false,
                Activo = true
            },
            new Horario
            {
                Id = 4,
                Nombre = "00:00",
                HoraReferencia = new TimeOnly(0, 0),
                EsCierreDiaOperativoAnterior = true,
                Activo = true
            });

        builder.Entity<AmbienteMedicion>().HasData(CrearMedicionesIniciales(vigenteDesde));
        builder.Entity<AmbienteHorario>().HasData(CrearHorariosIniciales(vigenteDesde));
    }

    private static AmbienteMedicion[] CrearMedicionesIniciales(DateOnly vigenteDesde)
    {
        return
        [
            CrearAmbienteMedicion(1, 1, 1, 18m, 26m, vigenteDesde),
            CrearAmbienteMedicion(2, 1, 2, 30m, 70m, vigenteDesde),
            CrearAmbienteMedicion(3, 1, 3, 2m, 8m, vigenteDesde),
            CrearAmbienteMedicion(4, 2, 3, 2m, 8m, vigenteDesde),
            CrearAmbienteMedicion(5, 3, 1, 18m, 26m, vigenteDesde),
            CrearAmbienteMedicion(6, 3, 2, 30m, 70m, vigenteDesde),
            CrearAmbienteMedicion(7, 3, 3, 2m, 8m, vigenteDesde),
            CrearAmbienteMedicion(8, 4, 1, 18m, 26m, vigenteDesde),
            CrearAmbienteMedicion(9, 4, 2, 30m, 70m, vigenteDesde),
            CrearAmbienteMedicion(10, 4, 3, 2m, 8m, vigenteDesde),
            CrearAmbienteMedicion(11, 5, 1, 18m, 26m, vigenteDesde),
            CrearAmbienteMedicion(12, 5, 2, 30m, 70m, vigenteDesde),
            CrearAmbienteMedicion(13, 5, 3, 2m, 8m, vigenteDesde)
        ];
    }

    private static AmbienteMedicion CrearAmbienteMedicion(
        int id,
        int ambienteId,
        int tipoMedicionId,
        decimal minimo,
        decimal maximo,
        DateOnly vigenteDesde)
    {
        return new AmbienteMedicion
        {
            Id = id,
            AmbienteId = ambienteId,
            TipoMedicionId = tipoMedicionId,
            RangoMinimo = minimo,
            RangoMaximo = maximo,
            VigenteDesde = vigenteDesde,
            Activo = true
        };
    }

    private static AmbienteHorario[] CrearHorariosIniciales(DateOnly vigenteDesde)
    {
        var asignaciones = new (int AmbienteId, int HorarioId)[]
        {
            (1, 1), (1, 2), (1, 3),
            (2, 1), (2, 2), (2, 3), (2, 4),
            (3, 1), (3, 2), (3, 3), (3, 4),
            (4, 1), (4, 2), (4, 3), (4, 4),
            (5, 1), (5, 2), (5, 3)
        };

        return asignaciones
            .Select((asignacion, indice) => new AmbienteHorario
            {
                Id = indice + 1,
                AmbienteId = asignacion.AmbienteId,
                HorarioId = asignacion.HorarioId,
                MinutosAntes = 30,
                MinutosDespues = 60,
                VigenteDesde = vigenteDesde,
                Activo = true
            })
            .ToArray();
    }
}
