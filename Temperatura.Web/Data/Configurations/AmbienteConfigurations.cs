using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Temperatura.Web.Domain;

namespace Temperatura.Web.Data.Configurations;

public sealed class AmbienteConfiguration : IEntityTypeConfiguration<Ambiente>
{
    public void Configure(EntityTypeBuilder<Ambiente> entity)
    {
        entity.ToTable("Ambientes");
        entity.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        entity.HasIndex(x => x.Nombre).IsUnique();

        entity.HasData(
            new Ambiente { Id = 1, Nombre = "Farmacia", Activo = true },
            new Ambiente { Id = 2, Nombre = "Enfermería", Activo = true },
            new Ambiente { Id = 3, Nombre = "UMA 1", Activo = true },
            new Ambiente { Id = 4, Nombre = "UMA 2", Activo = true },
            new Ambiente { Id = 5, Nombre = "UMA 3", Activo = true });
    }
}

public sealed class TipoMedicionConfiguration : IEntityTypeConfiguration<TipoMedicion>
{
    public void Configure(EntityTypeBuilder<TipoMedicion> entity)
    {
        entity.ToTable("TiposMedicion", table =>
            table.HasCheckConstraint(
                "CK_TiposMedicion_Decimales",
                "[DecimalesPermitidos] BETWEEN 0 AND 4"));

        entity.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        entity.Property(x => x.SimboloUnidad).HasMaxLength(10).IsRequired();
        entity.HasIndex(x => x.Nombre).IsUnique();

        entity.HasData(
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
    }
}

public sealed class AmbienteMedicionConfiguration : IEntityTypeConfiguration<AmbienteMedicion>
{
    private static readonly DateOnly VigenteDesdeInicial = new(2026, 1, 1);

    public void Configure(EntityTypeBuilder<AmbienteMedicion> entity)
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

        entity.HasData(
            Crear(1, 1, 1, 15m, 30m),
            Crear(2, 1, 2, 15m, 65m),
            Crear(3, 1, 3, 2m, 8m),
            Crear(4, 2, 3, 2m, 8m),
            Crear(5, 3, 1, 15m, 30m),
            Crear(6, 3, 2, 15m, 65m),
            Crear(7, 3, 3, 2m, 8m),
            Crear(8, 4, 1, 15m, 30m),
            Crear(9, 4, 2, 15m, 65m),
            Crear(10, 4, 3, 2m, 8m),
            Crear(11, 5, 1, 15m, 30m),
            Crear(12, 5, 2, 15m, 65m),
            Crear(13, 5, 3, 2m, 8m));
    }

    private static AmbienteMedicion Crear(
        int id,
        int ambienteId,
        int tipoMedicionId,
        decimal minimo,
        decimal maximo)
    {
        return new AmbienteMedicion
        {
            Id = id,
            AmbienteId = ambienteId,
            TipoMedicionId = tipoMedicionId,
            RangoMinimo = minimo,
            RangoMaximo = maximo,
            VigenteDesde = VigenteDesdeInicial,
            Activo = true
        };
    }
}
