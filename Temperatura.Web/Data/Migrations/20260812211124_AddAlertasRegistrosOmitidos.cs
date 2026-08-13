using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Temperatura.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertasRegistrosOmitidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertasRegistrosOmitidos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaOperativa = table.Column<DateOnly>(type: "date", nullable: false),
                    AmbienteId = table.Column<int>(type: "int", nullable: false),
                    HorarioId = table.Column<int>(type: "int", nullable: false),
                    FechaHoraCierre = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaHoraDeteccion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IntentosEnvio = table.Column<int>(type: "int", nullable: false),
                    FechaHoraEnvio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UltimoError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertasRegistrosOmitidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertasRegistrosOmitidos_Ambientes_AmbienteId",
                        column: x => x.AmbienteId,
                        principalTable: "Ambientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AlertasRegistrosOmitidos_Horarios_HorarioId",
                        column: x => x.HorarioId,
                        principalTable: "Horarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertasRegistrosOmitidos_AmbienteId",
                table: "AlertasRegistrosOmitidos",
                column: "AmbienteId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasRegistrosOmitidos_Estado",
                table: "AlertasRegistrosOmitidos",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasRegistrosOmitidos_FechaOperativa_AmbienteId_HorarioId",
                table: "AlertasRegistrosOmitidos",
                columns: new[] { "FechaOperativa", "AmbienteId", "HorarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlertasRegistrosOmitidos_HorarioId",
                table: "AlertasRegistrosOmitidos",
                column: "HorarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertasRegistrosOmitidos");
        }
    }
}
