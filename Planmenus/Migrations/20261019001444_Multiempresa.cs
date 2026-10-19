using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planmenus.Migrations
{
    /// <inheritdoc />
    public partial class Multiempresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Serie",
                table: "Serie");

            migrationBuilder.DropIndex(
                name: "IX_Comprobante_comp_tipo_comp_serie_comp_numero",
                table: "Comprobante");

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Zona",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Turno_Caja",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Tipo_Pago",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Ticketera",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Serie",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Rol_Usuario",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Rol_Permisos",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Rol",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Reserva_Mesa",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Reserva",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Mesas",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Menu",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Inicial",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Ingresos",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Impresion_Cola",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Generales",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Facturacion",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Eventos",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Egresos",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Delivery",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Correo_Log",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Comprobante_Detalle",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Comprobante",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Caja_Detalle",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Caja",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "Asignacion_Mozo",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Serie",
                table: "Serie",
                columns: new[] { "id_empresa", "serie_clave" });

            migrationBuilder.CreateTable(
                name: "Empresa",
                columns: table => new
                {
                    id_empresa = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    emp_nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    emp_codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    emp_ruc = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    emp_activa = table.Column<bool>(type: "bit", nullable: false),
                    emp_creada = table.Column<DateTime>(type: "datetime2", nullable: false),
                    emp_plan = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresa", x => x.id_empresa);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Zona_id_empresa",
                table: "Zona",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_id_empresa",
                table: "Usuarios",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Turno_Caja_id_empresa",
                table: "Turno_Caja",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Tipo_Pago_id_empresa",
                table: "Tipo_Pago",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Ticketera_id_empresa",
                table: "Ticketera",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Serie_id_empresa",
                table: "Serie",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Rol_Usuario_id_empresa",
                table: "Rol_Usuario",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Rol_Permisos_id_empresa",
                table: "Rol_Permisos",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Rol_id_empresa",
                table: "Rol",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Reserva_Mesa_id_empresa",
                table: "Reserva_Mesa",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Reserva_id_empresa",
                table: "Reserva",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Mesas_id_empresa",
                table: "Mesas",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Menu_id_empresa",
                table: "Menu",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Inicial_id_empresa",
                table: "Inicial",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Ingresos_id_empresa",
                table: "Ingresos",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Impresion_Cola_id_empresa",
                table: "Impresion_Cola",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Generales_id_empresa",
                table: "Generales",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Facturacion_id_empresa",
                table: "Facturacion",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_id_empresa",
                table: "Eventos",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Egresos_id_empresa",
                table: "Egresos",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Delivery_id_empresa",
                table: "Delivery",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Correo_Log_id_empresa",
                table: "Correo_Log",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobante_Detalle_id_empresa",
                table: "Comprobante_Detalle",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobante_id_empresa",
                table: "Comprobante",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobante_id_empresa_comp_tipo_comp_serie_comp_numero",
                table: "Comprobante",
                columns: new[] { "id_empresa", "comp_tipo", "comp_serie", "comp_numero" },
                unique: true,
                filter: "[comp_tipo] IS NOT NULL AND [comp_serie] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Caja_Detalle_id_empresa",
                table: "Caja_Detalle",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Caja_id_empresa",
                table: "Caja",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Asignacion_Mozo_id_empresa",
                table: "Asignacion_Mozo",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Empresa_emp_codigo",
                table: "Empresa",
                column: "emp_codigo",
                unique: true);

            // Empresa 1: dueña de todos los datos existentes (toma el nombre de Configuracion si existe)
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT dbo.Empresa ON;
INSERT INTO dbo.Empresa (id_empresa, emp_nombre, emp_codigo, emp_ruc, emp_activa, emp_creada, emp_plan)
SELECT 1, ISNULL((SELECT TOP 1 gene_empre FROM dbo.Generales ORDER BY id_gene), N'Mi restaurante'), N'principal',
       (SELECT TOP 1 gene_ruc FROM dbo.Generales ORDER BY id_gene), 1, SYSDATETIME(), N'basico'
WHERE NOT EXISTS (SELECT 1 FROM dbo.Empresa WHERE id_empresa = 1);
SET IDENTITY_INSERT dbo.Empresa OFF;");

            migrationBuilder.AddForeignKey(
                name: "FK_Asignacion_Mozo_Empresa_id_empresa",
                table: "Asignacion_Mozo",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Caja_Empresa_id_empresa",
                table: "Caja",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Caja_Detalle_Empresa_id_empresa",
                table: "Caja_Detalle",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Comprobante_Empresa_id_empresa",
                table: "Comprobante",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Comprobante_Detalle_Empresa_id_empresa",
                table: "Comprobante_Detalle",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Correo_Log_Empresa_id_empresa",
                table: "Correo_Log",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Delivery_Empresa_id_empresa",
                table: "Delivery",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Egresos_Empresa_id_empresa",
                table: "Egresos",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Eventos_Empresa_id_empresa",
                table: "Eventos",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Facturacion_Empresa_id_empresa",
                table: "Facturacion",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Generales_Empresa_id_empresa",
                table: "Generales",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Impresion_Cola_Empresa_id_empresa",
                table: "Impresion_Cola",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ingresos_Empresa_id_empresa",
                table: "Ingresos",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inicial_Empresa_id_empresa",
                table: "Inicial",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Menu_Empresa_id_empresa",
                table: "Menu",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Mesas_Empresa_id_empresa",
                table: "Mesas",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reserva_Empresa_id_empresa",
                table: "Reserva",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reserva_Mesa_Empresa_id_empresa",
                table: "Reserva_Mesa",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rol_Empresa_id_empresa",
                table: "Rol",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rol_Permisos_Empresa_id_empresa",
                table: "Rol_Permisos",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rol_Usuario_Empresa_id_empresa",
                table: "Rol_Usuario",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Serie_Empresa_id_empresa",
                table: "Serie",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ticketera_Empresa_id_empresa",
                table: "Ticketera",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tipo_Pago_Empresa_id_empresa",
                table: "Tipo_Pago",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Turno_Caja_Empresa_id_empresa",
                table: "Turno_Caja",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Empresa_id_empresa",
                table: "Usuarios",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Zona_Empresa_id_empresa",
                table: "Zona",
                column: "id_empresa",
                principalTable: "Empresa",
                principalColumn: "id_empresa",
                onDelete: ReferentialAction.Restrict);

            // Reportes: siempre de una sola empresa
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[SP_CAJA_Q01] @FECINI DATE, @FECFIN DATE, @ID_EMPRESA INT AS
BEGIN
    SET NOCOUNT ON;
    SELECT tp.id_tipago, tp.tipago_desc, COUNT(c.id_caja) AS cant_tippag, ISNULL(SUM(c.caja_monto), 0) AS caja_monto,
           CONVERT(varchar(10), CAST(c.caja_fecha AS date), 103) AS caja_fecha
    FROM dbo.Caja c
    INNER JOIN dbo.Tipo_Pago tp ON tp.id_tipago = c.id_tipago
    WHERE c.id_empresa = @ID_EMPRESA AND c.caja_act = 1 AND c.caja_pagado = 1 AND CAST(c.caja_fecha AS date) BETWEEN @FECINI AND @FECFIN
    GROUP BY tp.id_tipago, tp.tipago_desc, CAST(c.caja_fecha AS date)
    ORDER BY CAST(c.caja_fecha AS date), tp.tipago_desc;
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[SP_CAJA_Q02] @FECINI DATE, @FECFIN DATE, @ID_EMPRESA INT AS
BEGIN
    SET NOCOUNT ON;
    SELECT tp.tipago_desc,
           ISNULL((SELECT STRING_AGG(CONCAT(d.det_cant, ' x ', m.menu_desc), ', ') WITHIN GROUP (ORDER BY d.id_detalle)
                   FROM dbo.Caja_Detalle d INNER JOIN dbo.Menu m ON m.id_menu = d.id_menu WHERE d.id_caja = c.id_caja), '') AS caja_plato,
           c.caja_monto, CONVERT(varchar(16), c.caja_fecha, 120) AS caja_fecha
    FROM dbo.Caja c
    INNER JOIN dbo.Tipo_Pago tp ON tp.id_tipago = c.id_tipago
    WHERE c.id_empresa = @ID_EMPRESA AND c.caja_act = 1 AND c.caja_pagado = 1 AND CAST(c.caja_fecha AS date) BETWEEN @FECINI AND @FECFIN
    ORDER BY tp.tipago_desc, c.caja_fecha;
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[SP_CAJA_Q03] @FECINI DATE, @FECFIN DATE, @ID_EMPRESA INT AS
BEGIN
    SET NOCOUNT ON;
    SELECT m.id_menu, m.menu_desc, SUM(d.det_cant) AS cant_menu, ISNULL(SUM(d.det_subtotal), 0) AS caja_monto,
           CONVERT(varchar(10), CAST(c.caja_fecha AS date), 103) AS caja_fecha
    FROM dbo.Caja c
    INNER JOIN dbo.Caja_Detalle d ON d.id_caja = c.id_caja
    INNER JOIN dbo.Menu m ON m.id_menu = d.id_menu
    WHERE c.id_empresa = @ID_EMPRESA AND c.caja_act = 1 AND c.caja_pagado = 1 AND CAST(c.caja_fecha AS date) BETWEEN @FECINI AND @FECFIN
    GROUP BY m.id_menu, m.menu_desc, CAST(c.caja_fecha AS date)
    ORDER BY CAST(c.caja_fecha AS date), m.menu_desc;
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[SP_CAJA_Q04] @FECINI DATE, @FECFIN DATE, @ID_EMPRESA INT AS
BEGIN
    SET NOCOUNT ON;
    SELECT CONCAT(d.det_cant, ' x ', m.menu_desc) AS menu_desc, CAST(c.id_caja AS varchar(20)) AS caja_ticket,
           d.det_subtotal AS caja_monto, CONVERT(varchar(16), c.caja_fecha, 120) AS caja_fecha
    FROM dbo.Caja c
    INNER JOIN dbo.Caja_Detalle d ON d.id_caja = c.id_caja
    INNER JOIN dbo.Menu m ON m.id_menu = d.id_menu
    WHERE c.id_empresa = @ID_EMPRESA AND c.caja_act = 1 AND c.caja_pagado = 1 AND CAST(c.caja_fecha AS date) BETWEEN @FECINI AND @FECFIN
    ORDER BY m.menu_desc, c.id_caja;
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[SP_CAJA_Q05] @FECINI DATE, @FECFIN DATE, @ID_EMPRESA INT AS
BEGIN
    SET NOCOUNT ON;
    WITH mov AS (
        SELECT CAST(ini_fecha AS date) AS dia, ini_monto AS inicial, 0 AS ventas, 0 AS ingresos, 0 AS egresos FROM dbo.Inicial WHERE id_empresa = @ID_EMPRESA
        UNION ALL
        SELECT CAST(caja_fecha AS date), 0, caja_monto, 0, 0 FROM dbo.Caja WHERE id_empresa = @ID_EMPRESA AND caja_act = 1 AND caja_pagado = 1
        UNION ALL
        SELECT CAST(ingre_fecha AS date), 0, 0, ingre_monto, 0 FROM dbo.Ingresos WHERE id_empresa = @ID_EMPRESA
        UNION ALL
        SELECT CAST(egre_fecha AS date), 0, 0, 0, egre_costo FROM dbo.Egresos WHERE id_empresa = @ID_EMPRESA
    )
    SELECT CONVERT(varchar(10), dia, 103) AS fecha,
           CAST(SUM(inicial) AS decimal(18,2)) AS Inicial, CAST(SUM(ventas) AS decimal(18,2)) AS Ventas,
           CAST(SUM(ingresos) AS decimal(18,2)) AS Ingresos, CAST(SUM(egresos) AS decimal(18,2)) AS Egresos,
           CAST(SUM(inicial) + SUM(ventas) + SUM(ingresos) - SUM(egresos) AS decimal(18,2)) AS Total
    FROM mov
    WHERE dia BETWEEN @FECINI AND @FECFIN
    GROUP BY dia
    ORDER BY dia;
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[SP_CAJA_Q06] @FECINI DATE, @FECFIN DATE, @ID_EMPRESA INT AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.id_caja, ISNULL(tp.tipago_desc, N'Por cobrar') AS tipago_desc, d.deliv_desc, ms.mesa_desc, c.caja_obs, c.caja_monto,
           CONVERT(varchar(16), c.caja_fecha, 120) AS caja_fecha, c.caja_act, c.caja_usuario, c.caja_pagado,
           ISNULL((SELECT STRING_AGG(CONCAT(cd.det_cant, ' x ', m.menu_desc), ', ') WITHIN GROUP (ORDER BY cd.id_detalle)
                   FROM dbo.Caja_Detalle cd INNER JOIN dbo.Menu m ON m.id_menu = cd.id_menu WHERE cd.id_caja = c.id_caja), '') AS detalle
    FROM dbo.Caja c
    LEFT JOIN dbo.Tipo_Pago tp ON tp.id_tipago = c.id_tipago
    LEFT JOIN dbo.Delivery d ON d.id_deliv = c.id_deliv
    LEFT JOIN dbo.Mesas ms ON ms.id_mesa = c.id_mesa
    WHERE c.id_empresa = @ID_EMPRESA AND CAST(c.caja_fecha AS date) BETWEEN @FECINI AND @FECFIN
    ORDER BY c.id_caja DESC;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asignacion_Mozo_Empresa_id_empresa",
                table: "Asignacion_Mozo");

            migrationBuilder.DropForeignKey(
                name: "FK_Caja_Empresa_id_empresa",
                table: "Caja");

            migrationBuilder.DropForeignKey(
                name: "FK_Caja_Detalle_Empresa_id_empresa",
                table: "Caja_Detalle");

            migrationBuilder.DropForeignKey(
                name: "FK_Comprobante_Empresa_id_empresa",
                table: "Comprobante");

            migrationBuilder.DropForeignKey(
                name: "FK_Comprobante_Detalle_Empresa_id_empresa",
                table: "Comprobante_Detalle");

            migrationBuilder.DropForeignKey(
                name: "FK_Correo_Log_Empresa_id_empresa",
                table: "Correo_Log");

            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_Empresa_id_empresa",
                table: "Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_Egresos_Empresa_id_empresa",
                table: "Egresos");

            migrationBuilder.DropForeignKey(
                name: "FK_Eventos_Empresa_id_empresa",
                table: "Eventos");

            migrationBuilder.DropForeignKey(
                name: "FK_Facturacion_Empresa_id_empresa",
                table: "Facturacion");

            migrationBuilder.DropForeignKey(
                name: "FK_Generales_Empresa_id_empresa",
                table: "Generales");

            migrationBuilder.DropForeignKey(
                name: "FK_Impresion_Cola_Empresa_id_empresa",
                table: "Impresion_Cola");

            migrationBuilder.DropForeignKey(
                name: "FK_Ingresos_Empresa_id_empresa",
                table: "Ingresos");

            migrationBuilder.DropForeignKey(
                name: "FK_Inicial_Empresa_id_empresa",
                table: "Inicial");

            migrationBuilder.DropForeignKey(
                name: "FK_Menu_Empresa_id_empresa",
                table: "Menu");

            migrationBuilder.DropForeignKey(
                name: "FK_Mesas_Empresa_id_empresa",
                table: "Mesas");

            migrationBuilder.DropForeignKey(
                name: "FK_Reserva_Empresa_id_empresa",
                table: "Reserva");

            migrationBuilder.DropForeignKey(
                name: "FK_Reserva_Mesa_Empresa_id_empresa",
                table: "Reserva_Mesa");

            migrationBuilder.DropForeignKey(
                name: "FK_Rol_Empresa_id_empresa",
                table: "Rol");

            migrationBuilder.DropForeignKey(
                name: "FK_Rol_Permisos_Empresa_id_empresa",
                table: "Rol_Permisos");

            migrationBuilder.DropForeignKey(
                name: "FK_Rol_Usuario_Empresa_id_empresa",
                table: "Rol_Usuario");

            migrationBuilder.DropForeignKey(
                name: "FK_Serie_Empresa_id_empresa",
                table: "Serie");

            migrationBuilder.DropForeignKey(
                name: "FK_Ticketera_Empresa_id_empresa",
                table: "Ticketera");

            migrationBuilder.DropForeignKey(
                name: "FK_Tipo_Pago_Empresa_id_empresa",
                table: "Tipo_Pago");

            migrationBuilder.DropForeignKey(
                name: "FK_Turno_Caja_Empresa_id_empresa",
                table: "Turno_Caja");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Empresa_id_empresa",
                table: "Usuarios");

            migrationBuilder.DropForeignKey(
                name: "FK_Zona_Empresa_id_empresa",
                table: "Zona");

            migrationBuilder.DropTable(
                name: "Empresa");

            migrationBuilder.DropIndex(
                name: "IX_Zona_id_empresa",
                table: "Zona");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_id_empresa",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Turno_Caja_id_empresa",
                table: "Turno_Caja");

            migrationBuilder.DropIndex(
                name: "IX_Tipo_Pago_id_empresa",
                table: "Tipo_Pago");

            migrationBuilder.DropIndex(
                name: "IX_Ticketera_id_empresa",
                table: "Ticketera");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Serie",
                table: "Serie");

            migrationBuilder.DropIndex(
                name: "IX_Serie_id_empresa",
                table: "Serie");

            migrationBuilder.DropIndex(
                name: "IX_Rol_Usuario_id_empresa",
                table: "Rol_Usuario");

            migrationBuilder.DropIndex(
                name: "IX_Rol_Permisos_id_empresa",
                table: "Rol_Permisos");

            migrationBuilder.DropIndex(
                name: "IX_Rol_id_empresa",
                table: "Rol");

            migrationBuilder.DropIndex(
                name: "IX_Reserva_Mesa_id_empresa",
                table: "Reserva_Mesa");

            migrationBuilder.DropIndex(
                name: "IX_Reserva_id_empresa",
                table: "Reserva");

            migrationBuilder.DropIndex(
                name: "IX_Mesas_id_empresa",
                table: "Mesas");

            migrationBuilder.DropIndex(
                name: "IX_Menu_id_empresa",
                table: "Menu");

            migrationBuilder.DropIndex(
                name: "IX_Inicial_id_empresa",
                table: "Inicial");

            migrationBuilder.DropIndex(
                name: "IX_Ingresos_id_empresa",
                table: "Ingresos");

            migrationBuilder.DropIndex(
                name: "IX_Impresion_Cola_id_empresa",
                table: "Impresion_Cola");

            migrationBuilder.DropIndex(
                name: "IX_Generales_id_empresa",
                table: "Generales");

            migrationBuilder.DropIndex(
                name: "IX_Facturacion_id_empresa",
                table: "Facturacion");

            migrationBuilder.DropIndex(
                name: "IX_Eventos_id_empresa",
                table: "Eventos");

            migrationBuilder.DropIndex(
                name: "IX_Egresos_id_empresa",
                table: "Egresos");

            migrationBuilder.DropIndex(
                name: "IX_Delivery_id_empresa",
                table: "Delivery");

            migrationBuilder.DropIndex(
                name: "IX_Correo_Log_id_empresa",
                table: "Correo_Log");

            migrationBuilder.DropIndex(
                name: "IX_Comprobante_Detalle_id_empresa",
                table: "Comprobante_Detalle");

            migrationBuilder.DropIndex(
                name: "IX_Comprobante_id_empresa",
                table: "Comprobante");

            migrationBuilder.DropIndex(
                name: "IX_Comprobante_id_empresa_comp_tipo_comp_serie_comp_numero",
                table: "Comprobante");

            migrationBuilder.DropIndex(
                name: "IX_Caja_Detalle_id_empresa",
                table: "Caja_Detalle");

            migrationBuilder.DropIndex(
                name: "IX_Caja_id_empresa",
                table: "Caja");

            migrationBuilder.DropIndex(
                name: "IX_Asignacion_Mozo_id_empresa",
                table: "Asignacion_Mozo");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Zona");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Turno_Caja");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Tipo_Pago");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Ticketera");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Serie");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Rol_Usuario");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Rol_Permisos");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Rol");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Reserva_Mesa");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Reserva");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Menu");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Inicial");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Ingresos");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Impresion_Cola");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Generales");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Facturacion");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Eventos");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Egresos");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Correo_Log");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Comprobante_Detalle");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Comprobante");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Caja_Detalle");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Caja");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "Asignacion_Mozo");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Serie",
                table: "Serie",
                column: "serie_clave");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobante_comp_tipo_comp_serie_comp_numero",
                table: "Comprobante",
                columns: new[] { "comp_tipo", "comp_serie", "comp_numero" },
                unique: true,
                filter: "[comp_tipo] IS NOT NULL AND [comp_serie] IS NOT NULL");
        }
    }
}
