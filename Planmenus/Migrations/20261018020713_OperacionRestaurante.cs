using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planmenus.Migrations
{
    /// <inheritdoc />
    public partial class OperacionRestaurante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "tipago_efectivo",
                table: "Tipo_Pago",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "id_tipago",
                table: "Caja",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<byte>(
                name: "caja_cocina",
                table: "Caja",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)2);

            migrationBuilder.AddColumn<DateTime>(
                name: "caja_fecha_pago",
                table: "Caja",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "caja_pagado",
                table: "Caja",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "Turno_Caja",
                columns: table => new
                {
                    id_turno = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    turno_usuario_apertura = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    turno_apertura = table.Column<DateTime>(type: "datetime2", nullable: false),
                    turno_monto_inicial = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    turno_usuario_cierre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    turno_cierre = table.Column<DateTime>(type: "datetime2", nullable: true),
                    turno_esperado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    turno_contado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    turno_diferencia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    turno_obs = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    turno_abierto = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turno_Caja", x => x.id_turno);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Caja_caja_pagado_caja_act",
                table: "Caja",
                columns: new[] { "caja_pagado", "caja_act" });

            // Ventas existentes: ya cobradas y entregadas; el efectivo se identifica por su nombre
            migrationBuilder.Sql(@"
UPDATE dbo.Caja SET caja_fecha_pago = caja_fecha WHERE caja_fecha_pago IS NULL AND caja_pagado = 1;
UPDATE dbo.Tipo_Pago SET tipago_efectivo = 1 WHERE tipago_desc LIKE N'%efectivo%';");

            // Los reportes solo consideran pedidos cobrados (los pedidos abiertos aun no son venta)
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[SP_CAJA_Q01] @FECINI DATE, @FECFIN DATE AS
BEGIN
    SET NOCOUNT ON;
    SELECT tp.id_tipago, tp.tipago_desc,
           COUNT(c.id_caja) AS cant_tippag,
           ISNULL(SUM(c.caja_monto), 0) AS caja_monto,
           CONVERT(varchar(10), CAST(c.caja_fecha AS date), 103) AS caja_fecha
    FROM dbo.Caja c
    INNER JOIN dbo.Tipo_Pago tp ON tp.id_tipago = c.id_tipago
    WHERE c.caja_act = 1 AND c.caja_pagado = 1 AND CAST(c.caja_fecha AS date) BETWEEN @FECINI AND @FECFIN
    GROUP BY tp.id_tipago, tp.tipago_desc, CAST(c.caja_fecha AS date)
    ORDER BY CAST(c.caja_fecha AS date), tp.tipago_desc;
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[SP_CAJA_Q02] @FECINI DATE, @FECFIN DATE AS
BEGIN
    SET NOCOUNT ON;
    SELECT tp.tipago_desc,
           ISNULL((SELECT STRING_AGG(CONCAT(d.det_cant, ' x ', m.menu_desc), ', ') WITHIN GROUP (ORDER BY d.id_detalle)
                   FROM dbo.Caja_Detalle d INNER JOIN dbo.Menu m ON m.id_menu = d.id_menu
                   WHERE d.id_caja = c.id_caja), '') AS caja_plato,
           c.caja_monto,
           CONVERT(varchar(16), c.caja_fecha, 120) AS caja_fecha
    FROM dbo.Caja c
    INNER JOIN dbo.Tipo_Pago tp ON tp.id_tipago = c.id_tipago
    WHERE c.caja_act = 1 AND c.caja_pagado = 1 AND CAST(c.caja_fecha AS date) BETWEEN @FECINI AND @FECFIN
    ORDER BY tp.tipago_desc, c.caja_fecha;
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[SP_CAJA_Q03] @FECINI DATE, @FECFIN DATE AS
BEGIN
    SET NOCOUNT ON;
    SELECT m.id_menu, m.menu_desc,
           SUM(d.det_cant) AS cant_menu,
           ISNULL(SUM(d.det_subtotal), 0) AS caja_monto,
           CONVERT(varchar(10), CAST(c.caja_fecha AS date), 103) AS caja_fecha
    FROM dbo.Caja c
    INNER JOIN dbo.Caja_Detalle d ON d.id_caja = c.id_caja
    INNER JOIN dbo.Menu m ON m.id_menu = d.id_menu
    WHERE c.caja_act = 1 AND c.caja_pagado = 1 AND CAST(c.caja_fecha AS date) BETWEEN @FECINI AND @FECFIN
    GROUP BY m.id_menu, m.menu_desc, CAST(c.caja_fecha AS date)
    ORDER BY CAST(c.caja_fecha AS date), m.menu_desc;
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[SP_CAJA_Q04] @FECINI DATE, @FECFIN DATE AS
BEGIN
    SET NOCOUNT ON;
    SELECT CONCAT(d.det_cant, ' x ', m.menu_desc) AS menu_desc, CAST(c.id_caja AS varchar(20)) AS caja_ticket,
           d.det_subtotal AS caja_monto,
           CONVERT(varchar(16), c.caja_fecha, 120) AS caja_fecha
    FROM dbo.Caja c
    INNER JOIN dbo.Caja_Detalle d ON d.id_caja = c.id_caja
    INNER JOIN dbo.Menu m ON m.id_menu = d.id_menu
    WHERE c.caja_act = 1 AND c.caja_pagado = 1 AND CAST(c.caja_fecha AS date) BETWEEN @FECINI AND @FECFIN
    ORDER BY m.menu_desc, c.id_caja;
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[SP_CAJA_Q05] @FECINI DATE, @FECFIN DATE AS
BEGIN
    SET NOCOUNT ON;
    WITH mov AS (
        SELECT CAST(ini_fecha AS date) AS dia, ini_monto AS inicial, 0 AS ventas, 0 AS ingresos, 0 AS egresos FROM dbo.Inicial
        UNION ALL
        SELECT CAST(caja_fecha AS date), 0, caja_monto, 0, 0 FROM dbo.Caja WHERE caja_act = 1 AND caja_pagado = 1
        UNION ALL
        SELECT CAST(ingre_fecha AS date), 0, 0, ingre_monto, 0 FROM dbo.Ingresos
        UNION ALL
        SELECT CAST(egre_fecha AS date), 0, 0, 0, egre_costo FROM dbo.Egresos
    )
    SELECT CONVERT(varchar(10), dia, 103) AS fecha,
           CAST(SUM(inicial) AS decimal(18,2)) AS Inicial,
           CAST(SUM(ventas) AS decimal(18,2)) AS Ventas,
           CAST(SUM(ingresos) AS decimal(18,2)) AS Ingresos,
           CAST(SUM(egresos) AS decimal(18,2)) AS Egresos,
           CAST(SUM(inicial) + SUM(ventas) + SUM(ingresos) - SUM(egresos) AS decimal(18,2)) AS Total
    FROM mov
    WHERE dia BETWEEN @FECINI AND @FECFIN
    GROUP BY dia
    ORDER BY dia;
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[SP_CAJA_Q06] @FECINI DATE, @FECFIN DATE AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.id_caja, ISNULL(tp.tipago_desc, N'Por cobrar') AS tipago_desc, d.deliv_desc, ms.mesa_desc, c.caja_obs, c.caja_monto,
           CONVERT(varchar(16), c.caja_fecha, 120) AS caja_fecha, c.caja_act, c.caja_usuario, c.caja_pagado,
           ISNULL((SELECT STRING_AGG(CONCAT(cd.det_cant, ' x ', m.menu_desc), ', ') WITHIN GROUP (ORDER BY cd.id_detalle)
                   FROM dbo.Caja_Detalle cd INNER JOIN dbo.Menu m ON m.id_menu = cd.id_menu
                   WHERE cd.id_caja = c.id_caja), '') AS detalle
    FROM dbo.Caja c
    LEFT JOIN dbo.Tipo_Pago tp ON tp.id_tipago = c.id_tipago
    LEFT JOIN dbo.Delivery d ON d.id_deliv = c.id_deliv
    LEFT JOIN dbo.Mesas ms ON ms.id_mesa = c.id_mesa
    WHERE CAST(c.caja_fecha AS date) BETWEEN @FECINI AND @FECFIN
    ORDER BY c.id_caja DESC;
END");

            // Submenus de operacion: Salon (mesas), Cocina y Apertura/cierre de caja. Activos solo para el Administrador.
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT dbo.Submenu ON;
IF NOT EXISTS (SELECT 1 FROM dbo.Submenu WHERE id_submenu = 25)
    INSERT INTO dbo.Submenu (id_submenu, id_menprin, submenu_desc, submenu_nro) VALUES (25, 3, N'Salón y mesas', N'8');
IF NOT EXISTS (SELECT 1 FROM dbo.Submenu WHERE id_submenu = 26)
    INSERT INTO dbo.Submenu (id_submenu, id_menprin, submenu_desc, submenu_nro) VALUES (26, 3, N'Cocina', N'9');
IF NOT EXISTS (SELECT 1 FROM dbo.Submenu WHERE id_submenu = 27)
    INSERT INTO dbo.Submenu (id_submenu, id_menprin, submenu_desc, submenu_nro) VALUES (27, 3, N'Apertura y cierre de caja', N'10');
SET IDENTITY_INSERT dbo.Submenu OFF;
INSERT INTO dbo.Rol_Permisos (id_rol, id_menprin, id_submenu, rolper_act)
SELECT r.id_rol, 3, s.id_submenu, CASE WHEN r.id_rol = 1 THEN 1 ELSE 0 END
FROM dbo.Rol r CROSS JOIN (VALUES (25), (26), (27)) s(id_submenu)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Rol_Permisos p WHERE p.id_rol = r.id_rol AND p.id_submenu = s.id_submenu);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Turno_Caja");

            migrationBuilder.DropIndex(
                name: "IX_Caja_caja_pagado_caja_act",
                table: "Caja");

            migrationBuilder.DropColumn(
                name: "tipago_efectivo",
                table: "Tipo_Pago");

            migrationBuilder.DropColumn(
                name: "caja_cocina",
                table: "Caja");

            migrationBuilder.DropColumn(
                name: "caja_fecha_pago",
                table: "Caja");

            migrationBuilder.DropColumn(
                name: "caja_pagado",
                table: "Caja");

            migrationBuilder.AlterColumn<int>(
                name: "id_tipago",
                table: "Caja",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
