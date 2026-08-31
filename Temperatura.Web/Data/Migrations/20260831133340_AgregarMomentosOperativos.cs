using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Temperatura.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMomentosOperativos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsCierreDiaOperativoAnteriorAplicado",
                table: "Registros",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "HoraReferenciaAplicada",
                table: "Registros",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<string>(
                name: "HorarioNombreAplicado",
                table: "Registros",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MomentoOperativoAplicado",
                table: "Registros",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MomentoOperativo",
                table: "Horarios",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Horarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "MomentoOperativo",
                value: "Manana");

            migrationBuilder.UpdateData(
                table: "Horarios",
                keyColumn: "Id",
                keyValue: 2,
                column: "MomentoOperativo",
                value: "Mediodia");

            migrationBuilder.UpdateData(
                table: "Horarios",
                keyColumn: "Id",
                keyValue: 3,
                column: "MomentoOperativo",
                value: "Noche");

            migrationBuilder.UpdateData(
                table: "Horarios",
                keyColumn: "Id",
                keyValue: 4,
                column: "MomentoOperativo",
                value: "Medianoche");

            migrationBuilder.Sql(
                """
                UPDATE [Horarios]
                SET [MomentoOperativo] = CASE
                    WHEN [EsCierreDiaOperativoAnterior] = 1 THEN 'Medianoche'
                    WHEN [HoraReferencia] < CAST('10:00:00' AS time) THEN 'Manana'
                    WHEN [HoraReferencia] < CAST('18:00:00' AS time) THEN 'Mediodia'
                    ELSE 'Noche'
                END;

                UPDATE registro
                SET
                    [HorarioNombreAplicado] = horario.[Nombre],
                    [HoraReferenciaAplicada] = horario.[HoraReferencia],
                    [MomentoOperativoAplicado] = horario.[MomentoOperativo],
                    [EsCierreDiaOperativoAnteriorAplicado] = horario.[EsCierreDiaOperativoAnterior]
                FROM [Registros] AS registro
                INNER JOIN [Horarios] AS horario ON horario.[Id] = registro.[HorarioId];
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Registros_MomentoOperativoAplicado",
                table: "Registros",
                sql: "[MomentoOperativoAplicado] IN ('Manana', 'Mediodia', 'Noche', 'Medianoche')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Horarios_MomentoOperativo",
                table: "Horarios",
                sql: "[MomentoOperativo] IN ('Manana', 'Mediodia', 'Noche', 'Medianoche')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Registros_MomentoOperativoAplicado",
                table: "Registros");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Horarios_MomentoOperativo",
                table: "Horarios");

            migrationBuilder.DropColumn(
                name: "EsCierreDiaOperativoAnteriorAplicado",
                table: "Registros");

            migrationBuilder.DropColumn(
                name: "HoraReferenciaAplicada",
                table: "Registros");

            migrationBuilder.DropColumn(
                name: "HorarioNombreAplicado",
                table: "Registros");

            migrationBuilder.DropColumn(
                name: "MomentoOperativoAplicado",
                table: "Registros");

            migrationBuilder.DropColumn(
                name: "MomentoOperativo",
                table: "Horarios");
        }
    }
}
