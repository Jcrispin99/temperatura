using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Temperatura.Web.Domain;
using Temperatura.Web.Domain.Enums;

namespace Temperatura.Web.Data.Configurations;

public sealed class HorarioConfiguration : IEntityTypeConfiguration<Horario>
{
    public void Configure(EntityTypeBuilder<Horario> entity)
    {
        entity.ToTable("Horarios", table =>
            table.HasCheckConstraint(
                "CK_Horarios_MomentoOperativo",
                "[MomentoOperativo] IN ('Manana', 'Mediodia', 'Noche', 'Medianoche')"));
        entity.Property(x => x.Nombre).HasMaxLength(50).IsRequired();
        entity.Property(x => x.MomentoOperativo).HasConversion<string>().HasMaxLength(20);
        entity.HasIndex(x => x.HoraReferencia).IsUnique();

        entity.HasData(
            new Horario
            {
                Id = 1,
                Nombre = "07:00",
                HoraReferencia = new TimeOnly(7, 0),
                MomentoOperativo = MomentoOperativo.Manana,
                EsCierreDiaOperativoAnterior = false,
                Activo = true
            },
            new Horario
            {
                Id = 2,
                Nombre = "13:00",
                HoraReferencia = new TimeOnly(13, 0),
                MomentoOperativo = MomentoOperativo.Mediodia,
                EsCierreDiaOperativoAnterior = false,
                Activo = true
            },
            new Horario
            {
                Id = 3,
                Nombre = "19:00",
                HoraReferencia = new TimeOnly(19, 0),
                MomentoOperativo = MomentoOperativo.Noche,
                EsCierreDiaOperativoAnterior = false,
                Activo = true
            },
            new Horario
            {
                Id = 4,
                Nombre = "01:00",
                HoraReferencia = new TimeOnly(1, 0),
                MomentoOperativo = MomentoOperativo.Medianoche,
                EsCierreDiaOperativoAnterior = true,
                Activo = true
            });
    }
}

public sealed class AmbienteHorarioConfiguration : IEntityTypeConfiguration<AmbienteHorario>
{
    private static readonly DateOnly VigenteDesdeInicial = new(2026, 1, 1);

    public void Configure(EntityTypeBuilder<AmbienteHorario> entity)
    {
        entity.ToTable("AmbientesHorarios", table =>
        {
            table.HasCheckConstraint(
                "CK_AmbientesHorarios_Ventana",
                "[MinutosAntes] >= 0 AND [MinutosDespues] > 0 " +
                "AND [MinutosToleranciaPuntualidad] >= 0 " +
                "AND [MinutosToleranciaPuntualidad] <= [MinutosDespues] " +
                "AND [MinutosRegularizacion] BETWEEN 0 AND 2880");
            table.HasCheckConstraint(
                "CK_AmbientesHorarios_Vigencia",
                "[VigenteHasta] IS NULL OR [VigenteHasta] >= [VigenteDesde]");
        });

        entity.Property(x => x.MinutosRegularizacion)
            .HasDefaultValue(AmbienteHorario.MinutosRegularizacionPredeterminados);

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

        var asignaciones = new (int AmbienteId, int HorarioId)[]
        {
            (1, 1), (1, 2), (1, 3),
            (2, 1), (2, 2), (2, 3), (2, 4),
            (3, 1), (3, 2), (3, 3), (3, 4),
            (4, 1), (4, 2), (4, 3), (4, 4),
            (5, 1), (5, 2), (5, 3)
        };

        entity.HasData(asignaciones.Select((asignacion, indice) => new AmbienteHorario
        {
            Id = indice + 1,
            AmbienteId = asignacion.AmbienteId,
            HorarioId = asignacion.HorarioId,
            MinutosAntes = AmbienteHorario.MinutosAntesPredeterminados,
            MinutosToleranciaPuntualidad =
                AmbienteHorario.MinutosToleranciaPuntualidadPredeterminados,
            MinutosDespues = AmbienteHorario.MinutosDespuesPredeterminados,
            MinutosRegularizacion = AmbienteHorario.MinutosRegularizacionPredeterminados,
            VigenteDesde = VigenteDesdeInicial,
            Activo = true
        }));
    }
}
