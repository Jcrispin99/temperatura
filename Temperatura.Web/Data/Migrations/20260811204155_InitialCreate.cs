using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Temperatura.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ambientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ambientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Horarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HoraReferencia = table.Column<TimeOnly>(type: "time", nullable: false),
                    EsCierreDiaOperativoAnterior = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Horarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposMedicion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SimboloUnidad = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DecimalesPermitidos = table.Column<byte>(type: "tinyint", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposMedicion", x => x.Id);
                    table.CheckConstraint("CK_TiposMedicion_Decimales", "[DecimalesPermitidos] BETWEEN 0 AND 4");
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosAmbientes",
                columns: table => new
                {
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AmbienteId = table.Column<int>(type: "int", nullable: false),
                    EsPredeterminado = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosAmbientes", x => new { x.UsuarioId, x.AmbienteId });
                    table.CheckConstraint("CK_UsuariosAmbientes_PredeterminadoActivo", "[EsPredeterminado] = 0 OR [Activo] = 1");
                    table.ForeignKey(
                        name: "FK_UsuariosAmbientes_Ambientes_AmbienteId",
                        column: x => x.AmbienteId,
                        principalTable: "Ambientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsuariosAmbientes_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AmbientesHorarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AmbienteId = table.Column<int>(type: "int", nullable: false),
                    HorarioId = table.Column<int>(type: "int", nullable: false),
                    MinutosAntes = table.Column<short>(type: "smallint", nullable: false),
                    MinutosDespues = table.Column<short>(type: "smallint", nullable: false),
                    VigenteDesde = table.Column<DateOnly>(type: "date", nullable: false),
                    VigenteHasta = table.Column<DateOnly>(type: "date", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmbientesHorarios", x => x.Id);
                    table.CheckConstraint("CK_AmbientesHorarios_Ventana", "[MinutosAntes] >= 0 AND [MinutosDespues] > 0");
                    table.CheckConstraint("CK_AmbientesHorarios_Vigencia", "[VigenteHasta] IS NULL OR [VigenteHasta] >= [VigenteDesde]");
                    table.ForeignKey(
                        name: "FK_AmbientesHorarios_Ambientes_AmbienteId",
                        column: x => x.AmbienteId,
                        principalTable: "Ambientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AmbientesHorarios_Horarios_HorarioId",
                        column: x => x.HorarioId,
                        principalTable: "Horarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Registros",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaOperativa = table.Column<DateOnly>(type: "date", nullable: false),
                    AmbienteId = table.Column<int>(type: "int", nullable: false),
                    HorarioId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FechaHoraRegistro = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Puntualidad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Registros_Ambientes_AmbienteId",
                        column: x => x.AmbienteId,
                        principalTable: "Ambientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Registros_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Registros_Horarios_HorarioId",
                        column: x => x.HorarioId,
                        principalTable: "Horarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AmbientesMediciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AmbienteId = table.Column<int>(type: "int", nullable: false),
                    TipoMedicionId = table.Column<int>(type: "int", nullable: false),
                    RangoMinimo = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    RangoMaximo = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    VigenteDesde = table.Column<DateOnly>(type: "date", nullable: false),
                    VigenteHasta = table.Column<DateOnly>(type: "date", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmbientesMediciones", x => x.Id);
                    table.CheckConstraint("CK_AmbientesMediciones_Rango", "[RangoMinimo] <= [RangoMaximo]");
                    table.CheckConstraint("CK_AmbientesMediciones_Vigencia", "[VigenteHasta] IS NULL OR [VigenteHasta] >= [VigenteDesde]");
                    table.ForeignKey(
                        name: "FK_AmbientesMediciones_Ambientes_AmbienteId",
                        column: x => x.AmbienteId,
                        principalTable: "Ambientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AmbientesMediciones_TiposMedicion_TipoMedicionId",
                        column: x => x.TipoMedicionId,
                        principalTable: "TiposMedicion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesRegistro",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegistroId = table.Column<long>(type: "bigint", nullable: false),
                    AmbienteMedicionId = table.Column<int>(type: "int", nullable: false),
                    TipoMedicionId = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    LimiteMinimoAplicado = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    LimiteMaximoAplicado = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    EstadoRango = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesRegistro", x => x.Id);
                    table.CheckConstraint("CK_DetallesRegistro_Limites", "[LimiteMinimoAplicado] <= [LimiteMaximoAplicado]");
                    table.ForeignKey(
                        name: "FK_DetallesRegistro_AmbientesMediciones_AmbienteMedicionId",
                        column: x => x.AmbienteMedicionId,
                        principalTable: "AmbientesMediciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesRegistro_Registros_RegistroId",
                        column: x => x.RegistroId,
                        principalTable: "Registros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesRegistro_TiposMedicion_TipoMedicionId",
                        column: x => x.TipoMedicionId,
                        principalTable: "TiposMedicion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Ambientes",
                columns: new[] { "Id", "Activo", "Nombre" },
                values: new object[,]
                {
                    { 1, true, "Farmacia" },
                    { 2, true, "Enfermería" },
                    { 3, true, "UMA 1" },
                    { 4, true, "UMA 2" },
                    { 5, true, "UMA 3" }
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "a83bce0d-b0dc-4a0f-bb38-2d58fce2c001", "a83bce0d-b0dc-4a0f-bb38-2d58fce2c001", "Registrador", "REGISTRADOR" },
                    { "a83bce0d-b0dc-4a0f-bb38-2d58fce2c002", "a83bce0d-b0dc-4a0f-bb38-2d58fce2c002", "Supervisor", "SUPERVISOR" }
                });

            migrationBuilder.InsertData(
                table: "Horarios",
                columns: new[] { "Id", "Activo", "EsCierreDiaOperativoAnterior", "HoraReferencia", "Nombre" },
                values: new object[,]
                {
                    { 1, true, false, new TimeOnly(7, 0, 0), "07:00" },
                    { 2, true, false, new TimeOnly(12, 0, 0), "12:00" },
                    { 3, true, false, new TimeOnly(19, 0, 0), "19:00" },
                    { 4, true, true, new TimeOnly(0, 0, 0), "00:00" }
                });

            migrationBuilder.InsertData(
                table: "TiposMedicion",
                columns: new[] { "Id", "Activo", "DecimalesPermitidos", "Nombre", "SimboloUnidad" },
                values: new object[,]
                {
                    { 1, true, (byte)1, "Temperatura ambiental", "°C" },
                    { 2, true, (byte)1, "Humedad relativa", "%" },
                    { 3, true, (byte)1, "Temperatura de refrigeración", "°C" }
                });

            migrationBuilder.InsertData(
                table: "AmbientesHorarios",
                columns: new[] { "Id", "Activo", "AmbienteId", "HorarioId", "MinutosAntes", "MinutosDespues", "VigenteDesde", "VigenteHasta" },
                values: new object[,]
                {
                    { 1, true, 1, 1, (short)30, (short)60, new DateOnly(2026, 1, 1), null },
                    { 2, true, 1, 2, (short)30, (short)60, new DateOnly(2026, 1, 1), null },
                    { 3, true, 1, 3, (short)30, (short)60, new DateOnly(2026, 1, 1), null },
                    { 4, true, 2, 1, (short)30, (short)60, new DateOnly(2026, 1, 1), null },
                    { 5, true, 2, 2, (short)30, (short)60, new DateOnly(2026, 1, 1), null },
                    { 6, true, 2, 3, (short)30, (short)60, new DateOnly(2026, 1, 1), null },
                    { 7, true, 2, 4, (short)30, (short)60, new DateOnly(2026, 1, 1), null },
                    { 8, true, 3, 1, (short)30, (short)60, new DateOnly(2026, 1, 1), null },
                    { 9, true, 3, 2, (short)30, (short)60, new DateOnly(2026, 1, 1), null },
                    { 10, true, 3, 3, (short)30, (short)60, new DateOnly(2026, 1, 1), null },
                    { 11, true, 3, 4, (short)30, (short)60, new DateOnly(2026, 1, 1), null },
                    { 12, true, 4, 1, (short)30, (short)60, new DateOnly(2026, 1, 1), null },
                    { 13, true, 4, 2, (short)30, (short)60, new DateOnly(2026, 1, 1), null },
                    { 14, true, 4, 3, (short)30, (short)60, new DateOnly(2026, 1, 1), null },
                    { 15, true, 4, 4, (short)30, (short)60, new DateOnly(2026, 1, 1), null },
                    { 16, true, 5, 1, (short)30, (short)60, new DateOnly(2026, 1, 1), null },
                    { 17, true, 5, 2, (short)30, (short)60, new DateOnly(2026, 1, 1), null },
                    { 18, true, 5, 3, (short)30, (short)60, new DateOnly(2026, 1, 1), null }
                });

            migrationBuilder.InsertData(
                table: "AmbientesMediciones",
                columns: new[] { "Id", "Activo", "AmbienteId", "RangoMaximo", "RangoMinimo", "TipoMedicionId", "VigenteDesde", "VigenteHasta" },
                values: new object[,]
                {
                    { 1, true, 1, 26m, 18m, 1, new DateOnly(2026, 1, 1), null },
                    { 2, true, 1, 70m, 30m, 2, new DateOnly(2026, 1, 1), null },
                    { 3, true, 1, 8m, 2m, 3, new DateOnly(2026, 1, 1), null },
                    { 4, true, 2, 8m, 2m, 3, new DateOnly(2026, 1, 1), null },
                    { 5, true, 3, 26m, 18m, 1, new DateOnly(2026, 1, 1), null },
                    { 6, true, 3, 70m, 30m, 2, new DateOnly(2026, 1, 1), null },
                    { 7, true, 3, 8m, 2m, 3, new DateOnly(2026, 1, 1), null },
                    { 8, true, 4, 26m, 18m, 1, new DateOnly(2026, 1, 1), null },
                    { 9, true, 4, 70m, 30m, 2, new DateOnly(2026, 1, 1), null },
                    { 10, true, 4, 8m, 2m, 3, new DateOnly(2026, 1, 1), null },
                    { 11, true, 5, 26m, 18m, 1, new DateOnly(2026, 1, 1), null },
                    { 12, true, 5, 70m, 30m, 2, new DateOnly(2026, 1, 1), null },
                    { 13, true, 5, 8m, 2m, 3, new DateOnly(2026, 1, 1), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ambientes_Nombre",
                table: "Ambientes",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AmbientesHorarios_AmbienteId_HorarioId",
                table: "AmbientesHorarios",
                columns: new[] { "AmbienteId", "HorarioId" },
                unique: true,
                filter: "[Activo] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_AmbientesHorarios_AmbienteId_HorarioId_VigenteDesde",
                table: "AmbientesHorarios",
                columns: new[] { "AmbienteId", "HorarioId", "VigenteDesde" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AmbientesHorarios_HorarioId",
                table: "AmbientesHorarios",
                column: "HorarioId");

            migrationBuilder.CreateIndex(
                name: "IX_AmbientesMediciones_AmbienteId_TipoMedicionId",
                table: "AmbientesMediciones",
                columns: new[] { "AmbienteId", "TipoMedicionId" },
                unique: true,
                filter: "[Activo] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_AmbientesMediciones_AmbienteId_TipoMedicionId_VigenteDesde",
                table: "AmbientesMediciones",
                columns: new[] { "AmbienteId", "TipoMedicionId", "VigenteDesde" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AmbientesMediciones_TipoMedicionId",
                table: "AmbientesMediciones",
                column: "TipoMedicionId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesRegistro_AmbienteMedicionId",
                table: "DetallesRegistro",
                column: "AmbienteMedicionId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesRegistro_RegistroId_TipoMedicionId",
                table: "DetallesRegistro",
                columns: new[] { "RegistroId", "TipoMedicionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetallesRegistro_TipoMedicionId",
                table: "DetallesRegistro",
                column: "TipoMedicionId");

            migrationBuilder.CreateIndex(
                name: "IX_Horarios_HoraReferencia",
                table: "Horarios",
                column: "HoraReferencia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Registros_AmbienteId_HorarioId_FechaOperativa",
                table: "Registros",
                columns: new[] { "AmbienteId", "HorarioId", "FechaOperativa" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Registros_FechaOperativa_AmbienteId",
                table: "Registros",
                columns: new[] { "FechaOperativa", "AmbienteId" });

            migrationBuilder.CreateIndex(
                name: "IX_Registros_HorarioId",
                table: "Registros",
                column: "HorarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Registros_UsuarioId",
                table: "Registros",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_TiposMedicion_Nombre",
                table: "TiposMedicion",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosAmbientes_AmbienteId",
                table: "UsuariosAmbientes",
                column: "AmbienteId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosAmbientes_UsuarioId",
                table: "UsuariosAmbientes",
                column: "UsuarioId",
                unique: true,
                filter: "[EsPredeterminado] = 1 AND [Activo] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AmbientesHorarios");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "DetallesRegistro");

            migrationBuilder.DropTable(
                name: "UsuariosAmbientes");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AmbientesMediciones");

            migrationBuilder.DropTable(
                name: "Registros");

            migrationBuilder.DropTable(
                name: "TiposMedicion");

            migrationBuilder.DropTable(
                name: "Ambientes");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Horarios");
        }
    }
}
