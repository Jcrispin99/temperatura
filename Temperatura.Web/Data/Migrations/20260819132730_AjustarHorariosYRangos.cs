using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Temperatura.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AjustarHorariosYRangos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AmbientesMediciones",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "RangoMaximo", "RangoMinimo" },
                values: new object[] { 30m, 15m });

            migrationBuilder.UpdateData(
                table: "AmbientesMediciones",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "RangoMaximo", "RangoMinimo" },
                values: new object[] { 65m, 15m });

            migrationBuilder.UpdateData(
                table: "AmbientesMediciones",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "RangoMaximo", "RangoMinimo" },
                values: new object[] { 30m, 15m });

            migrationBuilder.UpdateData(
                table: "AmbientesMediciones",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "RangoMaximo", "RangoMinimo" },
                values: new object[] { 65m, 15m });

            migrationBuilder.UpdateData(
                table: "AmbientesMediciones",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "RangoMaximo", "RangoMinimo" },
                values: new object[] { 30m, 15m });

            migrationBuilder.UpdateData(
                table: "AmbientesMediciones",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "RangoMaximo", "RangoMinimo" },
                values: new object[] { 65m, 15m });

            migrationBuilder.UpdateData(
                table: "AmbientesMediciones",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "RangoMaximo", "RangoMinimo" },
                values: new object[] { 30m, 15m });

            migrationBuilder.UpdateData(
                table: "AmbientesMediciones",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "RangoMaximo", "RangoMinimo" },
                values: new object[] { 65m, 15m });

            migrationBuilder.UpdateData(
                table: "Horarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "HoraReferencia", "Nombre" },
                values: new object[] { new TimeOnly(7, 0, 0), "07:00" });

            migrationBuilder.UpdateData(
                table: "Horarios",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "HoraReferencia", "Nombre" },
                values: new object[] { new TimeOnly(19, 0, 0), "19:00" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AmbientesMediciones",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "RangoMaximo", "RangoMinimo" },
                values: new object[] { 26m, 18m });

            migrationBuilder.UpdateData(
                table: "AmbientesMediciones",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "RangoMaximo", "RangoMinimo" },
                values: new object[] { 70m, 30m });

            migrationBuilder.UpdateData(
                table: "AmbientesMediciones",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "RangoMaximo", "RangoMinimo" },
                values: new object[] { 26m, 18m });

            migrationBuilder.UpdateData(
                table: "AmbientesMediciones",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "RangoMaximo", "RangoMinimo" },
                values: new object[] { 70m, 30m });

            migrationBuilder.UpdateData(
                table: "AmbientesMediciones",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "RangoMaximo", "RangoMinimo" },
                values: new object[] { 26m, 18m });

            migrationBuilder.UpdateData(
                table: "AmbientesMediciones",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "RangoMaximo", "RangoMinimo" },
                values: new object[] { 70m, 30m });

            migrationBuilder.UpdateData(
                table: "AmbientesMediciones",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "RangoMaximo", "RangoMinimo" },
                values: new object[] { 26m, 18m });

            migrationBuilder.UpdateData(
                table: "AmbientesMediciones",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "RangoMaximo", "RangoMinimo" },
                values: new object[] { 70m, 30m });

            migrationBuilder.UpdateData(
                table: "Horarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "HoraReferencia", "Nombre" },
                values: new object[] { new TimeOnly(8, 0, 0), "08:00" });

            migrationBuilder.UpdateData(
                table: "Horarios",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "HoraReferencia", "Nombre" },
                values: new object[] { new TimeOnly(20, 0, 0), "20:00" });
        }
    }
}
