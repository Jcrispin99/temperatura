using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Temperatura.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarToleranciaPuntualidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AmbientesHorarios_Ventana",
                table: "AmbientesHorarios");

            migrationBuilder.AddColumn<short>(
                name: "MinutosToleranciaPuntualidad",
                table: "AmbientesHorarios",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "MinutosToleranciaPuntualidad",
                value: (short)30);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 2,
                column: "MinutosToleranciaPuntualidad",
                value: (short)30);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 3,
                column: "MinutosToleranciaPuntualidad",
                value: (short)30);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 4,
                column: "MinutosToleranciaPuntualidad",
                value: (short)30);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 5,
                column: "MinutosToleranciaPuntualidad",
                value: (short)30);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 6,
                column: "MinutosToleranciaPuntualidad",
                value: (short)30);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 7,
                column: "MinutosToleranciaPuntualidad",
                value: (short)30);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 8,
                column: "MinutosToleranciaPuntualidad",
                value: (short)30);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 9,
                column: "MinutosToleranciaPuntualidad",
                value: (short)30);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 10,
                column: "MinutosToleranciaPuntualidad",
                value: (short)30);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 11,
                column: "MinutosToleranciaPuntualidad",
                value: (short)30);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 12,
                column: "MinutosToleranciaPuntualidad",
                value: (short)30);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 13,
                column: "MinutosToleranciaPuntualidad",
                value: (short)30);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 14,
                column: "MinutosToleranciaPuntualidad",
                value: (short)30);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 15,
                column: "MinutosToleranciaPuntualidad",
                value: (short)30);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 16,
                column: "MinutosToleranciaPuntualidad",
                value: (short)30);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 17,
                column: "MinutosToleranciaPuntualidad",
                value: (short)30);

            migrationBuilder.UpdateData(
                table: "AmbientesHorarios",
                keyColumn: "Id",
                keyValue: 18,
                column: "MinutosToleranciaPuntualidad",
                value: (short)30);

            migrationBuilder.Sql(
                """
                UPDATE [AmbientesHorarios]
                SET [MinutosToleranciaPuntualidad] =
                    CASE
                        WHEN [MinutosDespues] < 30 THEN [MinutosDespues]
                        ELSE 30
                    END;

                DECLARE @FechaCambio date = '2026-08-23';
                DECLARE @Uma3Id int = (SELECT TOP (1) [Id] FROM [Ambientes] WHERE [Nombre] = N'UMA 3');
                DECLARE @Horario0745Id int =
                    (SELECT TOP (1) [Id] FROM [Horarios] WHERE [HoraReferencia] = '07:45');
                DECLARE @Horario1100Id int =
                    (SELECT TOP (1) [Id] FROM [Horarios] WHERE [HoraReferencia] = '11:00');

                IF @Horario0745Id IS NULL
                BEGIN
                    INSERT INTO [Horarios]
                        ([Nombre], [HoraReferencia], [EsCierreDiaOperativoAnterior], [Activo])
                    VALUES (N'07:45', '07:45', 0, 1);
                    SET @Horario0745Id = CONVERT(int, SCOPE_IDENTITY());
                END;

                IF @Horario1100Id IS NULL
                BEGIN
                    INSERT INTO [Horarios]
                        ([Nombre], [HoraReferencia], [EsCierreDiaOperativoAnterior], [Activo])
                    VALUES (N'11:00', '11:00', 0, 1);
                    SET @Horario1100Id = CONVERT(int, SCOPE_IDENTITY());
                END;

                IF @Uma3Id IS NOT NULL
                BEGIN
                    UPDATE ah
                    SET ah.[Activo] = 0,
                        ah.[VigenteHasta] =
                            CASE
                                WHEN ah.[VigenteDesde] < @FechaCambio
                                    THEN DATEADD(day, -1, @FechaCambio)
                                ELSE @FechaCambio
                            END
                    FROM [AmbientesHorarios] ah
                    INNER JOIN [Horarios] h ON h.[Id] = ah.[HorarioId]
                    WHERE ah.[AmbienteId] = @Uma3Id
                      AND ah.[Activo] = 1
                      AND h.[HoraReferencia] IN ('07:00', '13:00');

                    IF EXISTS (
                        SELECT 1
                        FROM [AmbientesHorarios]
                        WHERE [AmbienteId] = @Uma3Id
                          AND [HorarioId] = @Horario0745Id
                          AND [VigenteDesde] = @FechaCambio)
                    BEGIN
                        UPDATE [AmbientesHorarios]
                        SET [MinutosAntes] = 30,
                            [MinutosToleranciaPuntualidad] = 30,
                            [MinutosDespues] = 60,
                            [VigenteHasta] = NULL,
                            [Activo] = 1
                        WHERE [AmbienteId] = @Uma3Id
                          AND [HorarioId] = @Horario0745Id
                          AND [VigenteDesde] = @FechaCambio;
                    END
                    ELSE
                    BEGIN
                        INSERT INTO [AmbientesHorarios]
                            ([AmbienteId], [HorarioId], [MinutosAntes],
                             [MinutosToleranciaPuntualidad], [MinutosDespues],
                             [VigenteDesde], [VigenteHasta], [Activo])
                        VALUES
                            (@Uma3Id, @Horario0745Id, 30, 30, 60, @FechaCambio, NULL, 1);
                    END;

                    IF EXISTS (
                        SELECT 1
                        FROM [AmbientesHorarios]
                        WHERE [AmbienteId] = @Uma3Id
                          AND [HorarioId] = @Horario1100Id
                          AND [VigenteDesde] = @FechaCambio)
                    BEGIN
                        UPDATE [AmbientesHorarios]
                        SET [MinutosAntes] = 30,
                            [MinutosToleranciaPuntualidad] = 30,
                            [MinutosDespues] = 60,
                            [VigenteHasta] = NULL,
                            [Activo] = 1
                        WHERE [AmbienteId] = @Uma3Id
                          AND [HorarioId] = @Horario1100Id
                          AND [VigenteDesde] = @FechaCambio;
                    END
                    ELSE
                    BEGIN
                        INSERT INTO [AmbientesHorarios]
                            ([AmbienteId], [HorarioId], [MinutosAntes],
                             [MinutosToleranciaPuntualidad], [MinutosDespues],
                             [VigenteDesde], [VigenteHasta], [Activo])
                        VALUES
                            (@Uma3Id, @Horario1100Id, 30, 30, 60, @FechaCambio, NULL, 1);
                    END;
                END;

                UPDATE r
                SET r.[Puntualidad] = N'Puntual'
                FROM [Registros] r
                INNER JOIN [Horarios] h ON h.[Id] = r.[HorarioId]
                CROSS APPLY (
                    SELECT DATEADD(
                        minute,
                        DATEDIFF(minute, CAST('00:00' AS time), h.[HoraReferencia]),
                        DATEADD(
                            day,
                            CASE WHEN h.[EsCierreDiaOperativoAnterior] = 1 THEN 1 ELSE 0 END,
                            CAST(r.[FechaOperativa] AS datetime2))) AS [Referencia]
                ) calculo
                WHERE r.[Puntualidad] = N'Tardio'
                  AND CAST(r.[FechaHoraRegistro] AS datetime2) > calculo.[Referencia]
                  AND CAST(r.[FechaHoraRegistro] AS datetime2) <= DATEADD(minute, 30, calculo.[Referencia]);
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AmbientesHorarios_Ventana",
                table: "AmbientesHorarios",
                sql: "[MinutosAntes] >= 0 AND [MinutosDespues] > 0 AND [MinutosToleranciaPuntualidad] >= 0 AND [MinutosToleranciaPuntualidad] <= [MinutosDespues]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AmbientesHorarios_Ventana",
                table: "AmbientesHorarios");

            migrationBuilder.Sql(
                """
                DECLARE @Uma3Id int = (SELECT TOP (1) [Id] FROM [Ambientes] WHERE [Nombre] = N'UMA 3');
                DECLARE @Horario0745Id int =
                    (SELECT TOP (1) [Id] FROM [Horarios] WHERE [HoraReferencia] = '07:45');
                DECLARE @Horario1100Id int =
                    (SELECT TOP (1) [Id] FROM [Horarios] WHERE [HoraReferencia] = '11:00');

                DELETE FROM [AmbientesHorarios]
                WHERE [AmbienteId] = @Uma3Id
                  AND [HorarioId] IN (@Horario0745Id, @Horario1100Id)
                  AND [VigenteDesde] = '2026-08-23';

                ;WITH anteriores AS (
                    SELECT ah.[Id],
                           ROW_NUMBER() OVER (
                               PARTITION BY ah.[HorarioId]
                               ORDER BY ah.[VigenteDesde] DESC, ah.[Id] DESC) AS fila
                    FROM [AmbientesHorarios] ah
                    INNER JOIN [Horarios] h ON h.[Id] = ah.[HorarioId]
                    WHERE ah.[AmbienteId] = @Uma3Id
                      AND h.[HoraReferencia] IN ('07:00', '13:00')
                )
                UPDATE ah
                SET ah.[Activo] = CASE WHEN anteriores.fila = 1 THEN 1 ELSE 0 END,
                    ah.[VigenteHasta] = CASE WHEN anteriores.fila = 1 THEN NULL ELSE ah.[VigenteHasta] END
                FROM [AmbientesHorarios] ah
                INNER JOIN anteriores ON anteriores.[Id] = ah.[Id];

                DELETE FROM [Horarios]
                WHERE [Id] IN (@Horario0745Id, @Horario1100Id)
                  AND NOT EXISTS (SELECT 1 FROM [AmbientesHorarios] ah WHERE ah.[HorarioId] = [Horarios].[Id])
                  AND NOT EXISTS (SELECT 1 FROM [Registros] r WHERE r.[HorarioId] = [Horarios].[Id])
                  AND NOT EXISTS (SELECT 1 FROM [AlertasRegistrosOmitidos] a WHERE a.[HorarioId] = [Horarios].[Id]);
                """);

            migrationBuilder.DropColumn(
                name: "MinutosToleranciaPuntualidad",
                table: "AmbientesHorarios");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AmbientesHorarios_Ventana",
                table: "AmbientesHorarios",
                sql: "[MinutosAntes] >= 0 AND [MinutosDespues] > 0");
        }
    }
}
