using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planmenus.Migrations
{
    /// <summary>
    /// Procedimientos almacenados usados por SP_Menus. Antes solo existian en la base de datos;
    /// se reconstruyeron a partir de su uso en el codigo para poder versionarlos.
    /// </summary>
    public partial class StoredProcedures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reporte: pagos agrupados por tipo de pago y dia
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
    WHERE c.caja_act = 1 AND CAST(c.caja_fecha AS date) BETWEEN @FECINI AND @FECFIN
    GROUP BY tp.id_tipago, tp.tipago_desc, CAST(c.caja_fecha AS date)
    ORDER BY CAST(c.caja_fecha AS date), tp.tipago_desc;
END");

            // Reporte: detalle de ventas con su tipo de pago
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[SP_CAJA_Q02] @FECINI DATE, @FECFIN DATE AS
BEGIN
    SET NOCOUNT ON;
    SELECT tp.tipago_desc, m.menu_desc AS caja_plato, c.caja_monto,
           CONVERT(varchar(16), c.caja_fecha, 120) AS caja_fecha
    FROM dbo.Caja c
    INNER JOIN dbo.Tipo_Pago tp ON tp.id_tipago = c.id_tipago
    INNER JOIN dbo.Menu m ON m.id_menu = c.id_menu
    WHERE c.caja_act = 1 AND CAST(c.caja_fecha AS date) BETWEEN @FECINI AND @FECFIN
    ORDER BY tp.tipago_desc, c.caja_fecha;
END");

            // Reporte: ventas agrupadas por plato y dia
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[SP_CAJA_Q03] @FECINI DATE, @FECFIN DATE AS
BEGIN
    SET NOCOUNT ON;
    SELECT m.id_menu, m.menu_desc,
           COUNT(c.id_caja) AS cant_menu,
           ISNULL(SUM(c.caja_monto), 0) AS caja_monto,
           CONVERT(varchar(10), CAST(c.caja_fecha AS date), 103) AS caja_fecha
    FROM dbo.Caja c
    INNER JOIN dbo.Menu m ON m.id_menu = c.id_menu
    WHERE c.caja_act = 1 AND CAST(c.caja_fecha AS date) BETWEEN @FECINI AND @FECFIN
    GROUP BY m.id_menu, m.menu_desc, CAST(c.caja_fecha AS date)
    ORDER BY CAST(c.caja_fecha AS date), m.menu_desc;
END");

            // Reporte: detalle de tickets por plato
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[SP_CAJA_Q04] @FECINI DATE, @FECFIN DATE AS
BEGIN
    SET NOCOUNT ON;
    SELECT m.menu_desc, CAST(c.id_caja AS varchar(20)) AS caja_ticket, c.caja_monto,
           CONVERT(varchar(16), c.caja_fecha, 120) AS caja_fecha
    FROM dbo.Caja c
    INNER JOIN dbo.Menu m ON m.id_menu = c.id_menu
    WHERE c.caja_act = 1 AND CAST(c.caja_fecha AS date) BETWEEN @FECINI AND @FECFIN
    ORDER BY m.menu_desc, c.id_caja;
END");

            // Cuadre de caja por dia: inicial + ventas + ingresos - egresos
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[SP_CAJA_Q05] @FECINI DATE, @FECFIN DATE AS
BEGIN
    SET NOCOUNT ON;
    WITH mov AS (
        SELECT CAST(ini_fecha AS date) AS dia, ini_monto AS inicial, 0 AS ventas, 0 AS ingresos, 0 AS egresos FROM dbo.Inicial
        UNION ALL
        SELECT CAST(caja_fecha AS date), 0, caja_monto, 0, 0 FROM dbo.Caja WHERE caja_act = 1
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

            // Caja diaria: listado de ventas (activas y anuladas)
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[SP_CAJA_Q06] @FECINI DATE, @FECFIN DATE AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.id_caja, tp.tipago_desc, d.deliv_desc, ms.mesa_desc, c.caja_obs, c.caja_monto,
           CONVERT(varchar(16), c.caja_fecha, 120) AS caja_fecha, c.caja_act
    FROM dbo.Caja c
    LEFT JOIN dbo.Tipo_Pago tp ON tp.id_tipago = c.id_tipago
    LEFT JOIN dbo.Delivery d ON d.id_deliv = c.id_deliv
    LEFT JOIN dbo.Mesas ms ON ms.id_mesa = c.id_mesa
    WHERE CAST(c.caja_fecha AS date) BETWEEN @FECINI AND @FECFIN
    ORDER BY c.id_caja DESC;
END");

            // Sincronizar el evento del calendario con su menu
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[SP_EVENTOS_U01] @ID_MENU INT AS
BEGIN
    SET NOCOUNT ON;
    UPDATE e SET e.title = m.menu_desc, e.StarDate = m.menu_fecha, e.EndDate = m.menu_fecha
    FROM dbo.Eventos e
    INNER JOIN dbo.Menu m ON m.id_menu = e.id_menu
    WHERE e.id_menu = @ID_MENU;
END");

            // Eliminar el evento del calendario de un menu
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[SP_EVENTOS_D01] @ID_MENU INT AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.Eventos WHERE id_menu = @ID_MENU;
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sp in new[] { "SP_CAJA_Q01", "SP_CAJA_Q02", "SP_CAJA_Q03", "SP_CAJA_Q04", "SP_CAJA_Q05",
                                       "SP_CAJA_Q06", "SP_EVENTOS_U01", "SP_EVENTOS_D01" })
            {
                migrationBuilder.Sql($"DROP PROCEDURE IF EXISTS [dbo].[{sp}]");
            }
        }
    }
}
