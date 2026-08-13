using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Temperatura.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguracionSmtp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionesSmtp",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Servidor = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Puerto = table.Column<int>(type: "int", nullable: false),
                    UsarTls = table.Column<bool>(type: "bit", nullable: false),
                    CorreoRemitente = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NombreRemitente = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SecretoProtegido = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActualizadoPorUsuarioId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesSmtp", x => x.Id);
                    table.CheckConstraint("CK_ConfiguracionesSmtp_Puerto", "[Puerto] BETWEEN 1 AND 65535");
                    table.CheckConstraint("CK_ConfiguracionesSmtp_RegistroUnico", "[Id] = 1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionesSmtp");
        }
    }
}
