using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Temperatura.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAlertasRegistrosFueraRango : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertasRegistrosFueraRango",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegistroId = table.Column<long>(type: "bigint", nullable: false),
                    FechaHoraDeteccion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IntentosEnvio = table.Column<int>(type: "int", nullable: false),
                    FechaHoraEnvio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UltimoError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertasRegistrosFueraRango", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertasRegistrosFueraRango_Registros_RegistroId",
                        column: x => x.RegistroId,
                        principalTable: "Registros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertasRegistrosFueraRango_Estado",
                table: "AlertasRegistrosFueraRango",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasRegistrosFueraRango_RegistroId",
                table: "AlertasRegistrosFueraRango",
                column: "RegistroId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertasRegistrosFueraRango");
        }
    }
}
