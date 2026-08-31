using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Temperatura.Web.Domain;

namespace Temperatura.Web.Data.Configurations;

public sealed class ConfiguracionSmtpConfiguration : IEntityTypeConfiguration<ConfiguracionSmtp>
{
    public void Configure(EntityTypeBuilder<ConfiguracionSmtp> entity)
    {
        entity.ToTable("ConfiguracionesSmtp", table =>
        {
            table.HasCheckConstraint("CK_ConfiguracionesSmtp_RegistroUnico", "[Id] = 1");
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
    }
}
