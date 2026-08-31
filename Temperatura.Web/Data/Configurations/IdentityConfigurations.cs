using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Temperatura.Web.Domain;

namespace Temperatura.Web.Data.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> entity)
    {
        entity.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        entity.Property(x => x.Activo).HasDefaultValue(true);
    }
}

public sealed class UsuarioAmbienteConfiguration : IEntityTypeConfiguration<UsuarioAmbiente>
{
    public void Configure(EntityTypeBuilder<UsuarioAmbiente> entity)
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
    }
}

public sealed class IdentityRoleConfiguration : IEntityTypeConfiguration<IdentityRole>
{
    public void Configure(EntityTypeBuilder<IdentityRole> entity)
    {
        entity.HasData(
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
}
