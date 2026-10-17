using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planmenus.Migrations
{
    /// <inheritdoc />
    public partial class PedidosTicketeraRelaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Usua_user",
                table: "Usuarios",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gene_impresora_ip",
                table: "Generales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "gene_impresora_puerto",
                table: "Generales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "gene_ticket_ancho",
                table: "Generales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "gene_ticket_pie",
                table: "Generales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "id_menu",
                table: "Caja",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "caja_usuario",
                table: "Caja",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Caja_Detalle",
                columns: table => new
                {
                    id_detalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_caja = table.Column<int>(type: "int", nullable: false),
                    id_menu = table.Column<int>(type: "int", nullable: false),
                    det_cant = table.Column<int>(type: "int", nullable: false),
                    det_precio = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    det_subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Caja_Detalle", x => x.id_detalle);
                    table.ForeignKey(
                        name: "FK_Caja_Detalle_Caja_id_caja",
                        column: x => x.id_caja,
                        principalTable: "Caja",
                        principalColumn: "id_caja",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Caja_Detalle_Menu_id_menu",
                        column: x => x.id_menu,
                        principalTable: "Menu",
                        principalColumn: "id_menu",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Usua_user",
                table: "Usuarios",
                column: "Usua_user",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submenu_id_menprin",
                table: "Submenu",
                column: "id_menprin");

            migrationBuilder.CreateIndex(
                name: "IX_Rol_Usuario_id_rol",
                table: "Rol_Usuario",
                column: "id_rol");

            migrationBuilder.CreateIndex(
                name: "IX_Rol_Usuario_IdUsuario",
                table: "Rol_Usuario",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Rol_Permisos_id_menprin",
                table: "Rol_Permisos",
                column: "id_menprin");

            migrationBuilder.CreateIndex(
                name: "IX_Rol_Permisos_id_rol",
                table: "Rol_Permisos",
                column: "id_rol");

            migrationBuilder.CreateIndex(
                name: "IX_Rol_Permisos_id_submenu",
                table: "Rol_Permisos",
                column: "id_submenu");

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_id_menu",
                table: "Eventos",
                column: "id_menu");

            migrationBuilder.CreateIndex(
                name: "IX_Caja_caja_fecha",
                table: "Caja",
                column: "caja_fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Caja_id_deliv",
                table: "Caja",
                column: "id_deliv");

            migrationBuilder.CreateIndex(
                name: "IX_Caja_id_menu",
                table: "Caja",
                column: "id_menu");

            migrationBuilder.CreateIndex(
                name: "IX_Caja_id_mesa",
                table: "Caja",
                column: "id_mesa");

            migrationBuilder.CreateIndex(
                name: "IX_Caja_id_tipago",
                table: "Caja",
                column: "id_tipago");

            migrationBuilder.CreateIndex(
                name: "IX_Caja_Detalle_id_caja",
                table: "Caja_Detalle",
                column: "id_caja");

            migrationBuilder.CreateIndex(
                name: "IX_Caja_Detalle_id_menu",
                table: "Caja_Detalle",
                column: "id_menu");

            // Datos antiguos: limpiar referencias huerfanas para poder crear las llaves foraneas
            migrationBuilder.Sql(@"
UPDATE dbo.Caja SET id_deliv = NULL WHERE id_deliv IS NOT NULL AND id_deliv NOT IN (SELECT id_deliv FROM dbo.Delivery);
UPDATE dbo.Caja SET id_mesa = NULL WHERE id_mesa IS NOT NULL AND id_mesa NOT IN (SELECT id_mesa FROM dbo.Mesas);
UPDATE dbo.Caja SET id_menu = NULL WHERE id_menu IS NOT NULL AND id_menu NOT IN (SELECT id_menu FROM dbo.Menu);
IF EXISTS (SELECT 1 FROM dbo.Caja WHERE id_tipago NOT IN (SELECT id_tipago FROM dbo.Tipo_Pago))
BEGIN
    INSERT INTO dbo.Tipo_Pago (tipago_desc, tipago_act) VALUES (N'Sin especificar', 0);
    UPDATE dbo.Caja SET id_tipago = SCOPE_IDENTITY() WHERE id_tipago NOT IN (SELECT id_tipago FROM dbo.Tipo_Pago);
END
DELETE FROM dbo.Eventos WHERE id_menu NOT IN (SELECT id_menu FROM dbo.Menu);
DELETE FROM dbo.Rol_Permisos WHERE id_rol NOT IN (SELECT id_rol FROM dbo.Rol)
    OR id_menprin NOT IN (SELECT id_menprin FROM dbo.Menuprin)
    OR id_submenu NOT IN (SELECT id_submenu FROM dbo.Submenu);
DELETE FROM dbo.Rol_Usuario WHERE IdUsuario NOT IN (SELECT IdUsuario FROM dbo.Usuarios)
    OR id_rol NOT IN (SELECT id_rol FROM dbo.Rol);
DELETE FROM dbo.Submenu WHERE id_menprin NOT IN (SELECT id_menprin FROM dbo.Menuprin);");

            // Las ventas antiguas (un plato por ticket) pasan al detalle
            migrationBuilder.Sql(@"
INSERT INTO dbo.Caja_Detalle (id_caja, id_menu, det_cant, det_precio, det_subtotal)
SELECT c.id_caja, c.id_menu, 1, c.caja_monto, c.caja_monto
FROM dbo.Caja c
WHERE c.id_menu IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Caja_Detalle d WHERE d.id_caja = c.id_caja);");

            migrationBuilder.AddForeignKey(
                name: "FK_Caja_Delivery_id_deliv",
                table: "Caja",
                column: "id_deliv",
                principalTable: "Delivery",
                principalColumn: "id_deliv",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Caja_Menu_id_menu",
                table: "Caja",
                column: "id_menu",
                principalTable: "Menu",
                principalColumn: "id_menu",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Caja_Mesas_id_mesa",
                table: "Caja",
                column: "id_mesa",
                principalTable: "Mesas",
                principalColumn: "id_mesa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Caja_Tipo_Pago_id_tipago",
                table: "Caja",
                column: "id_tipago",
                principalTable: "Tipo_Pago",
                principalColumn: "id_tipago",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Eventos_Menu_id_menu",
                table: "Eventos",
                column: "id_menu",
                principalTable: "Menu",
                principalColumn: "id_menu",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rol_Permisos_Menuprin_id_menprin",
                table: "Rol_Permisos",
                column: "id_menprin",
                principalTable: "Menuprin",
                principalColumn: "id_menprin",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rol_Permisos_Rol_id_rol",
                table: "Rol_Permisos",
                column: "id_rol",
                principalTable: "Rol",
                principalColumn: "id_rol",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rol_Permisos_Submenu_id_submenu",
                table: "Rol_Permisos",
                column: "id_submenu",
                principalTable: "Submenu",
                principalColumn: "id_submenu",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rol_Usuario_Rol_id_rol",
                table: "Rol_Usuario",
                column: "id_rol",
                principalTable: "Rol",
                principalColumn: "id_rol",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rol_Usuario_Usuarios_IdUsuario",
                table: "Rol_Usuario",
                column: "IdUsuario",
                principalTable: "Usuarios",
                principalColumn: "IdUsuario",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Submenu_Menuprin_id_menprin",
                table: "Submenu",
                column: "id_menprin",
                principalTable: "Menuprin",
                principalColumn: "id_menprin",
                onDelete: ReferentialAction.Restrict);


            // Reportes por plato a partir del detalle del pedido
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
    WHERE c.caja_act = 1 AND CAST(c.caja_fecha AS date) BETWEEN @FECINI AND @FECFIN
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
    WHERE c.caja_act = 1 AND CAST(c.caja_fecha AS date) BETWEEN @FECINI AND @FECFIN
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
    WHERE c.caja_act = 1 AND CAST(c.caja_fecha AS date) BETWEEN @FECINI AND @FECFIN
    ORDER BY m.menu_desc, c.id_caja;
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[SP_CAJA_Q06] @FECINI DATE, @FECFIN DATE AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.id_caja, tp.tipago_desc, d.deliv_desc, ms.mesa_desc, c.caja_obs, c.caja_monto,
           CONVERT(varchar(16), c.caja_fecha, 120) AS caja_fecha, c.caja_act, c.caja_usuario,
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

            // Nuevo submenu: Carta digital con QR (Planificacion). Activo solo para el Administrador.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM dbo.Submenu WHERE id_submenu = 16)
BEGIN
    SET IDENTITY_INSERT dbo.Submenu ON;
    INSERT INTO dbo.Submenu (id_submenu, id_menprin, submenu_desc, submenu_nro) VALUES (16, 2, N'Carta digital (QR)', N'9');
    SET IDENTITY_INSERT dbo.Submenu OFF;
END
INSERT INTO dbo.Rol_Permisos (id_rol, id_menprin, id_submenu, rolper_act)
SELECT r.id_rol, 2, 16, CASE WHEN r.id_rol = 1 THEN 1 ELSE 0 END
FROM dbo.Rol r
WHERE NOT EXISTS (SELECT 1 FROM dbo.Rol_Permisos p WHERE p.id_rol = r.id_rol AND p.id_submenu = 16);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Caja_Delivery_id_deliv",
                table: "Caja");

            migrationBuilder.DropForeignKey(
                name: "FK_Caja_Menu_id_menu",
                table: "Caja");

            migrationBuilder.DropForeignKey(
                name: "FK_Caja_Mesas_id_mesa",
                table: "Caja");

            migrationBuilder.DropForeignKey(
                name: "FK_Caja_Tipo_Pago_id_tipago",
                table: "Caja");

            migrationBuilder.DropForeignKey(
                name: "FK_Eventos_Menu_id_menu",
                table: "Eventos");

            migrationBuilder.DropForeignKey(
                name: "FK_Rol_Permisos_Menuprin_id_menprin",
                table: "Rol_Permisos");

            migrationBuilder.DropForeignKey(
                name: "FK_Rol_Permisos_Rol_id_rol",
                table: "Rol_Permisos");

            migrationBuilder.DropForeignKey(
                name: "FK_Rol_Permisos_Submenu_id_submenu",
                table: "Rol_Permisos");

            migrationBuilder.DropForeignKey(
                name: "FK_Rol_Usuario_Rol_id_rol",
                table: "Rol_Usuario");

            migrationBuilder.DropForeignKey(
                name: "FK_Rol_Usuario_Usuarios_IdUsuario",
                table: "Rol_Usuario");

            migrationBuilder.DropForeignKey(
                name: "FK_Submenu_Menuprin_id_menprin",
                table: "Submenu");

            migrationBuilder.DropTable(
                name: "Caja_Detalle");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_Usua_user",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Submenu_id_menprin",
                table: "Submenu");

            migrationBuilder.DropIndex(
                name: "IX_Rol_Usuario_id_rol",
                table: "Rol_Usuario");

            migrationBuilder.DropIndex(
                name: "IX_Rol_Usuario_IdUsuario",
                table: "Rol_Usuario");

            migrationBuilder.DropIndex(
                name: "IX_Rol_Permisos_id_menprin",
                table: "Rol_Permisos");

            migrationBuilder.DropIndex(
                name: "IX_Rol_Permisos_id_rol",
                table: "Rol_Permisos");

            migrationBuilder.DropIndex(
                name: "IX_Rol_Permisos_id_submenu",
                table: "Rol_Permisos");

            migrationBuilder.DropIndex(
                name: "IX_Eventos_id_menu",
                table: "Eventos");

            migrationBuilder.DropIndex(
                name: "IX_Caja_caja_fecha",
                table: "Caja");

            migrationBuilder.DropIndex(
                name: "IX_Caja_id_deliv",
                table: "Caja");

            migrationBuilder.DropIndex(
                name: "IX_Caja_id_menu",
                table: "Caja");

            migrationBuilder.DropIndex(
                name: "IX_Caja_id_mesa",
                table: "Caja");

            migrationBuilder.DropIndex(
                name: "IX_Caja_id_tipago",
                table: "Caja");

            migrationBuilder.DropColumn(
                name: "gene_impresora_ip",
                table: "Generales");

            migrationBuilder.DropColumn(
                name: "gene_impresora_puerto",
                table: "Generales");

            migrationBuilder.DropColumn(
                name: "gene_ticket_ancho",
                table: "Generales");

            migrationBuilder.DropColumn(
                name: "gene_ticket_pie",
                table: "Generales");

            migrationBuilder.DropColumn(
                name: "caja_usuario",
                table: "Caja");

            migrationBuilder.AlterColumn<string>(
                name: "Usua_user",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "id_menu",
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
