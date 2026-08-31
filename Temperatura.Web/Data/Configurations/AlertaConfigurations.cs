using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Temperatura.Web.Domain;

namespace Temperatura.Web.Data.Configurations;

public sealed class AlertaRegistroOmitidoConfiguration : IEntityTypeConfiguration<AlertaRegistroOmitido>
{
    public void Configure(EntityTypeBuilder<AlertaRegistroOmitido> entity)
    {
        entity.ToTable("AlertasRegistrosOmitidos");
        entity.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        entity.Property(x => x.EstadoIncidencia).HasConversion<string>().HasMaxLength(30);
        entity.Property(x => x.UltimoError).HasMaxLength(1000);
        entity.Property(x => x.RevisadoPorUsuarioId).HasMaxLength(450);
        entity.Property(x => x.ComentarioRevision).HasMaxLength(1000);
        entity.HasIndex(x => new { x.FechaOperativa, x.AmbienteId, x.HorarioId }).IsUnique();
        entity.HasIndex(x => x.Estado);
        entity.HasIndex(x => x.EstadoIncidencia);
        entity.HasIndex(x => x.RegistroRegularizacionId)
            .IsUnique()
            .HasFilter("[RegistroRegularizacionId] IS NOT NULL");

        entity.HasOne(x => x.Ambiente)
            .WithMany()
            .HasForeignKey(x => x.AmbienteId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.Horario)
            .WithMany()
            .HasForeignKey(x => x.HorarioId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.RegistroRegularizacion)
            .WithOne(x => x.IncidenciaRegularizada)
            .HasForeignKey<AlertaRegistroOmitido>(x => x.RegistroRegularizacionId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.RevisadoPorUsuario)
            .WithMany()
            .HasForeignKey(x => x.RevisadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AlertaRegistroFueraRangoConfiguration
    : IEntityTypeConfiguration<AlertaRegistroFueraRango>
{
    public void Configure(EntityTypeBuilder<AlertaRegistroFueraRango> entity)
    {
        entity.ToTable("AlertasRegistrosFueraRango");
        entity.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        entity.Property(x => x.UltimoError).HasMaxLength(1000);
        entity.HasIndex(x => x.RegistroId).IsUnique();
        entity.HasIndex(x => x.Estado);

        entity.HasOne(x => x.Registro)
            .WithOne(x => x.AlertaFueraRango)
            .HasForeignKey<AlertaRegistroFueraRango>(x => x.RegistroId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
