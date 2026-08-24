using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Temperatura.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarRegularizacionEIncidencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AmbientesHorarios_Ventana",
                table: "AmbientesHorarios");

            migrationBuilder.AddColumn<string>(
                name: "MotivoFueraDePlazo",
                table: "Registros",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "MinutosRegularizacion",
                table: "AmbientesHorarios",
                type: "smallint",
                nullable: false,
                defaultValue: (short)720);

            migrationBuilder.AddColumn<string>(
                name: "ComentarioRevision",
                table: "AlertasRegistrosOmitidos",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstadoIncidencia",
                table: "AlertasRegistrosOmitidos",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "PendienteRegistro");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FechaHoraRegularizacion",
                table: "AlertasRegistrosOmitidos",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FechaHoraRevision",
                table: "AlertasRegistrosOmitidos",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RegistroRegularizacionId",
                table: "AlertasRegistrosOmitidos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevisadoPorUsuarioId",
                table: "AlertasRegistrosOmitidos",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "MinutosRegularizacion",
                value: (short)720);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 2,
                column: "MinutosRegularizacion",
                value: (short)720);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 3,
                column: "MinutosRegularizacion",
                value: (short)720);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 4,
                column: "MinutosRegularizacion",
                value: (short)720);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 5,
                column: "MinutosRegularizacion",
                value: (short)720);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 6,
                column: "MinutosRegularizacion",
                value: (short)720);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 7,
                column: "MinutosRegularizacion",
                value: (short)720);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 8,
                column: "MinutosRegularizacion",
                value: (short)720);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 9,
                column: "MinutosRegularizacion",
                value: (short)720);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 10,
                column: "MinutosRegularizacion",
                value: (short)720);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 11,
                column: "MinutosRegularizacion",
                value: (short)720);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 12,
                column: "MinutosRegularizacion",
                value: (short)720);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 13,
                column: "MinutosRegularizacion",
                value: (short)720);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 14,
                column: "MinutosRegularizacion",
                value: (short)720);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 15,
                column: "MinutosRegularizacion",
                value: (short)720);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 16,
                column: "MinutosRegularizacion",
                value: (short)720);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 17,
                column: "MinutosRegularizacion",
                value: (short)720);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 18,
                column: "MinutosRegularizacion",
                value: (short)720);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AmbientesHorarios_Ventana",
                table: "AmbientesHorarios",
                sql: "[MinutosAntes] >= 0 AND [MinutosDespues] > 0 AND [MinutosToleranciaPuntualidad] >= 0 AND [MinutosToleranciaPuntualidad] <= [MinutosDespues] AND [MinutosRegularizacion] BETWEEN 0 AND 2880");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasRegistrosOmitidos_EstadoIncidencia",
                table: "AlertasRegistrosOmitidos",
                column: "EstadoIncidencia");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasRegistrosOmitidos_RegistroRegularizacionId",
                table: "AlertasRegistrosOmitidos",
                column: "RegistroRegularizacionId",
                unique: true,
                filter: "[RegistroRegularizacionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasRegistrosOmitidos_RevisadoPorUsuarioId",
                table: "AlertasRegistrosOmitidos",
                column: "RevisadoPorUsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_AlertasRegistrosOmitidos_AspNetUsers_RevisadoPorUsuarioId",
                table: "AlertasRegistrosOmitidos",
                column: "RevisadoPorUsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AlertasRegistrosOmitidos_Registros_RegistroRegularizacionId",
                table: "AlertasRegistrosOmitidos",
                column: "RegistroRegularizacionId",
                principalTable: "Registros",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AlertasRegistrosOmitidos_AspNetUsers_RevisadoPorUsuarioId",
                table: "AlertasRegistrosOmitidos");

            migrationBuilder.DropForeignKey(
                name: "FK_AlertasRegistrosOmitidos_Registros_RegistroRegularizacionId",
                table: "AlertasRegistrosOmitidos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AmbientesHorarios_Ventana",
                table: "AmbientesHorarios");

            migrationBuilder.DropIndex(
                name: "IX_AlertasRegistrosOmitidos_EstadoIncidencia",
                table: "AlertasRegistrosOmitidos");

            migrationBuilder.DropIndex(
                name: "IX_AlertasRegistrosOmitidos_RegistroRegularizacionId",
                table: "AlertasRegistrosOmitidos");

            migrationBuilder.DropIndex(
                name: "IX_AlertasRegistrosOmitidos_RevisadoPorUsuarioId",
                table: "AlertasRegistrosOmitidos");

            migrationBuilder.DropColumn(
                name: "MotivoFueraDePlazo",
                table: "Registros");

            migrationBuilder.DropColumn(
                name: "MinutosRegularizacion",
                table: "AmbientesHorarios");

            migrationBuilder.DropColumn(
                name: "ComentarioRevision",
                table: "AlertasRegistrosOmitidos");

            migrationBuilder.DropColumn(
                name: "EstadoIncidencia",
                table: "AlertasRegistrosOmitidos");

            migrationBuilder.DropColumn(
                name: "FechaHoraRegularizacion",
                table: "AlertasRegistrosOmitidos");

            migrationBuilder.DropColumn(
                name: "FechaHoraRevision",
                table: "AlertasRegistrosOmitidos");

            migrationBuilder.DropColumn(
                name: "RegistroRegularizacionId",
                table: "AlertasRegistrosOmitidos");

            migrationBuilder.DropColumn(
                name: "RevisadoPorUsuarioId",
                table: "AlertasRegistrosOmitidos");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AmbientesHorarios_Ventana",
                table: "AmbientesHorarios",
                sql: "[MinutosAntes] >= 0 AND [MinutosDespues] > 0 AND [MinutosToleranciaPuntualidad] >= 0 AND [MinutosToleranciaPuntualidad] <= [MinutosDespues]");
        }
    }
}
