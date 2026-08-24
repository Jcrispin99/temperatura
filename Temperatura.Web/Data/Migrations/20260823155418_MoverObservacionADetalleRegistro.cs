using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Temperatura.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoverObservacionADetalleRegistro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Observacion",
                table: "DetallesRegistro",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE detalle
                SET detalle.[Observacion] = registro.[Observacion]
                FROM [DetallesRegistro] detalle
                INNER JOIN [Registros] registro ON registro.[Id] = detalle.[RegistroId]
                WHERE registro.[Observacion] IS NOT NULL
                  AND detalle.[Id] = (
                      SELECT MIN(primero.[Id])
                      FROM [DetallesRegistro] primero
                      WHERE primero.[RegistroId] = registro.[Id]);
                """);

            migrationBuilder.DropColumn(
                name: "Observacion",
                table: "Registros");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Observacion",
                table: "Registros",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE registro
                SET registro.[Observacion] = detalle.[Observacion]
                FROM [Registros] registro
                INNER JOIN [DetallesRegistro] detalle ON detalle.[RegistroId] = registro.[Id]
                WHERE detalle.[Observacion] IS NOT NULL
                  AND detalle.[Id] = (
                      SELECT MIN(primero.[Id])
                      FROM [DetallesRegistro] primero
                      WHERE primero.[RegistroId] = registro.[Id]
                        AND primero.[Observacion] IS NOT NULL);
                """);

            migrationBuilder.DropColumn(
                name: "Observacion",
                table: "DetallesRegistro");
        }
    }
}
