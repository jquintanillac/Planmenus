using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planmenus.Migrations
{
    /// <inheritdoc />
    public partial class FacturacionElectronica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Comprobante",
                columns: table => new
                {
                    id_comp = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_caja = table.Column<int>(type: "int", nullable: true),
                    comp_tipo = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    comp_serie = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    comp_numero = table.Column<int>(type: "int", nullable: false),
                    comp_fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    cli_tipo_doc = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    cli_num_doc = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    cli_nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    cli_direccion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    cli_email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    comp_gravada = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    comp_igv = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    comp_total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    comp_tasa_igv = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    comp_hash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    comp_estado = table.Column<byte>(type: "tinyint", nullable: false),
                    comp_sunat_codigo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    comp_sunat_mensaje = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    comp_ambiente = table.Column<byte>(type: "tinyint", nullable: false),
                    comp_xml = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    comp_cdr = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    comp_intentos = table.Column<int>(type: "int", nullable: false),
                    comp_enviado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ref_id_comp = table.Column<int>(type: "int", nullable: true),
                    nc_motivo = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    nc_sustento = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comprobante", x => x.id_comp);
                    table.ForeignKey(
                        name: "FK_Comprobante_Caja_id_caja",
                        column: x => x.id_caja,
                        principalTable: "Caja",
                        principalColumn: "id_caja",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Comprobante_Comprobante_ref_id_comp",
                        column: x => x.ref_id_comp,
                        principalTable: "Comprobante",
                        principalColumn: "id_comp",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FriendlyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Xml = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Facturacion",
                columns: table => new
                {
                    id_fe = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    fe_activa = table.Column<bool>(type: "bit", nullable: false),
                    fe_proveedor = table.Column<byte>(type: "tinyint", nullable: false),
                    fe_ambiente = table.Column<byte>(type: "tinyint", nullable: false),
                    fe_ruc = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    fe_razon_social = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    fe_nombre_comercial = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    fe_direccion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    fe_ubigeo = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    fe_establecimiento = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    fe_usuario_sol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    fe_clave_sol = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    fe_certificado = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    fe_certificado_clave = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    fe_certificado_titular = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    fe_certificado_vence = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fe_serie_boleta = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    fe_serie_factura = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    fe_serie_nc_boleta = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    fe_serie_nc_factura = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    fe_igv = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    fe_por_defecto = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facturacion", x => x.id_fe);
                });

            migrationBuilder.CreateTable(
                name: "Serie",
                columns: table => new
                {
                    serie_clave = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    serie_ultimo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Serie", x => x.serie_clave);
                });

            migrationBuilder.CreateTable(
                name: "Comprobante_Detalle",
                columns: table => new
                {
                    id_compdet = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_comp = table.Column<int>(type: "int", nullable: false),
                    item = table.Column<int>(type: "int", nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    cantidad = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    precio = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    valor_unitario = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    valor_venta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    igv = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    total = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comprobante_Detalle", x => x.id_compdet);
                    table.ForeignKey(
                        name: "FK_Comprobante_Detalle_Comprobante_id_comp",
                        column: x => x.id_comp,
                        principalTable: "Comprobante",
                        principalColumn: "id_comp",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comprobante_comp_estado_comp_fecha",
                table: "Comprobante",
                columns: new[] { "comp_estado", "comp_fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Comprobante_comp_tipo_comp_serie_comp_numero",
                table: "Comprobante",
                columns: new[] { "comp_tipo", "comp_serie", "comp_numero" },
                unique: true,
                filter: "[comp_tipo] IS NOT NULL AND [comp_serie] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobante_id_caja",
                table: "Comprobante",
                column: "id_caja");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobante_ref_id_comp",
                table: "Comprobante",
                column: "ref_id_comp");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobante_Detalle_id_comp",
                table: "Comprobante_Detalle",
                column: "id_comp");

            // Submenu: Comprobantes electronicos (Caja). Activo solo para el Administrador.
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT dbo.Submenu ON;
IF NOT EXISTS (SELECT 1 FROM dbo.Submenu WHERE id_submenu = 31)
    INSERT INTO dbo.Submenu (id_submenu, id_menprin, submenu_desc, submenu_nro) VALUES (31, 3, N'Comprobantes electrónicos', N'13');
SET IDENTITY_INSERT dbo.Submenu OFF;
INSERT INTO dbo.Rol_Permisos (id_rol, id_menprin, id_submenu, rolper_act)
SELECT r.id_rol, 3, 31, CASE WHEN r.id_rol = 1 THEN 1 ELSE 0 END
FROM dbo.Rol r
WHERE NOT EXISTS (SELECT 1 FROM dbo.Rol_Permisos p WHERE p.id_rol = r.id_rol AND p.id_submenu = 31);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Comprobante_Detalle");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "Facturacion");

            migrationBuilder.DropTable(
                name: "Serie");

            migrationBuilder.DropTable(
                name: "Comprobante");
        }
    }
}
