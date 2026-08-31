using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Temperatura.Web.Domain;

namespace Temperatura.Web.Data.Configurations;

public sealed class RegistroConfiguration : IEntityTypeConfiguration<Registro>
{
    public void Configure(EntityTypeBuilder<Registro> entity)
    {
        entity.ToTable("Registros", table =>
            table.HasCheckConstraint(
                "CK_Registros_MomentoOperativoAplicado",
                "[MomentoOperativoAplicado] IN ('Manana', 'Mediodia', 'Noche', 'Medianoche')"));
        entity.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        entity.Property(x => x.Puntualidad).HasConversion<string>().HasMaxLength(20);
        entity.Property(x => x.MotivoFueraDePlazo).HasMaxLength(500);
        entity.Property(x => x.HorarioNombreAplicado).HasMaxLength(50).IsRequired();
        entity.Property(x => x.MomentoOperativoAplicado).HasConversion<string>().HasMaxLength(20);
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
    }
}

public sealed class DetalleRegistroConfiguration : IEntityTypeConfiguration<DetalleRegistro>
{
    public void Configure(EntityTypeBuilder<DetalleRegistro> entity)
    {
        entity.ToTable("DetallesRegistro", table =>
            table.HasCheckConstraint(
                "CK_DetallesRegistro_Limites",
                "[LimiteMinimoAplicado] <= [LimiteMaximoAplicado]"));

        entity.Property(x => x.Valor).HasPrecision(9, 2);
        entity.Property(x => x.LimiteMinimoAplicado).HasPrecision(9, 2);
        entity.Property(x => x.LimiteMaximoAplicado).HasPrecision(9, 2);
        entity.Property(x => x.EstadoRango).HasConversion<string>().HasMaxLength(20);
        entity.Property(x => x.Observacion).HasMaxLength(500);
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
    }
}
