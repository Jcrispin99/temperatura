using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Temperatura.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class NombreDeTuMigracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Horarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "HoraReferencia", "Nombre" },
                values: new object[] { new TimeOnly(7, 0, 0), "07:00" });

            migrationBuilder.UpdateData(
                table: "Horarios",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "HoraReferencia", "Nombre" },
                values: new object[] { new TimeOnly(13, 0, 0), "13:00" });

            migrationBuilder.UpdateData(
                table: "Horarios",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "HoraReferencia", "Nombre" },
                values: new object[] { new TimeOnly(19, 0, 0), "19:00" });

            migrationBuilder.UpdateData(
                table: "Horarios",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "HoraReferencia", "Nombre" },
                values: new object[] { new TimeOnly(1, 0, 0), "01:00" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Horarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "HoraReferencia", "Nombre" },
                values: new object[] { new TimeOnly(7, 0, 0), "07:00" });

            migrationBuilder.UpdateData(
                table: "Horarios",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "HoraReferencia", "Nombre" },
                values: new object[] { new TimeOnly(12, 0, 0), "12:00" });

            migrationBuilder.UpdateData(
                table: "Horarios",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "HoraReferencia", "Nombre" },
                values: new object[] { new TimeOnly(19, 0, 0), "19:00" });

            migrationBuilder.UpdateData(
                table: "Horarios",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "HoraReferencia", "Nombre" },
                values: new object[] { new TimeOnly(0, 0, 0), "00:00" });
        }
    }
}
