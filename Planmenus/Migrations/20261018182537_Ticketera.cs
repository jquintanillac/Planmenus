using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planmenus.Migrations
{
    /// <inheritdoc />
    public partial class Ticketera : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Impresion_Cola",
                columns: table => new
                {
                    id_trabajo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    imp_creado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    imp_tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    id_caja = table.Column<int>(type: "int", nullable: true),
                    imp_impresora = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    imp_datos = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    imp_estado = table.Column<byte>(type: "tinyint", nullable: false),
                    imp_intentos = table.Column<int>(type: "int", nullable: false),
                    imp_tomado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    imp_terminado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    imp_error = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Impresion_Cola", x => x.id_trabajo);
                });

            migrationBuilder.CreateTable(
                name: "Ticketera",
                columns: table => new
                {
                    id_ticketera = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tk_modo = table.Column<byte>(type: "tinyint", nullable: false),
                    tk_impresora = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    tk_ip = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    tk_puerto = table.Column<int>(type: "int", nullable: false),
                    tk_ancho_mm = table.Column<int>(type: "int", nullable: false),
                    tk_columnas = table.Column<int>(type: "int", nullable: false),
                    tk_codepage = table.Column<int>(type: "int", nullable: false),
                    tk_logo_mostrar = table.Column<bool>(type: "bit", nullable: false),
                    tk_logo_png = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    tk_logo_raster = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    tk_logo_ancho = table.Column<int>(type: "int", nullable: false),
                    tk_logo_alto = table.Column<int>(type: "int", nullable: false),
                    tk_mostrar_ruc = table.Column<bool>(type: "bit", nullable: false),
                    tk_mostrar_direccion = table.Column<bool>(type: "bit", nullable: false),
                    tk_mostrar_telefono = table.Column<bool>(type: "bit", nullable: false),
                    tk_mostrar_mesa = table.Column<bool>(type: "bit", nullable: false),
                    tk_mostrar_mozo = table.Column<bool>(type: "bit", nullable: false),
                    tk_mostrar_obs = table.Column<bool>(type: "bit", nullable: false),
                    tk_total_grande = table.Column<bool>(type: "bit", nullable: false),
                    tk_pie = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    tk_corte = table.Column<byte>(type: "tinyint", nullable: false),
                    tk_avance = table.Column<int>(type: "int", nullable: false),
                    tk_copias = table.Column<int>(type: "int", nullable: false),
                    tk_gaveta = table.Column<bool>(type: "bit", nullable: false),
                    tk_auto = table.Column<bool>(type: "bit", nullable: false),
                    tk_agente_clave_hash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    tk_agente_contacto = table.Column<DateTime>(type: "datetime2", nullable: true),
                    tk_agente_equipo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    tk_agente_impresoras = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ticketera", x => x.id_ticketera);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Impresion_Cola_imp_estado_imp_creado",
                table: "Impresion_Cola",
                columns: new[] { "imp_estado", "imp_creado" });

            // Pasa la configuracion de ticket que estaba en Generales a la nueva tabla Ticketera
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM dbo.Ticketera)
INSERT INTO dbo.Ticketera (tk_modo, tk_ip, tk_puerto, tk_ancho_mm, tk_columnas, tk_codepage, tk_logo_mostrar, tk_logo_ancho, tk_logo_alto,
    tk_mostrar_ruc, tk_mostrar_direccion, tk_mostrar_telefono, tk_mostrar_mesa, tk_mostrar_mozo, tk_mostrar_obs, tk_total_grande,
    tk_pie, tk_corte, tk_avance, tk_copias, tk_gaveta, tk_auto)
SELECT TOP 1
    CASE WHEN NULLIF(g.gene_impresora_ip, '') IS NULL THEN 0 ELSE 1 END,
    NULLIF(g.gene_impresora_ip, ''), g.gene_impresora_puerto,
    CASE WHEN g.gene_ticket_ancho = 58 THEN 58 ELSE 80 END,
    CASE WHEN g.gene_ticket_ancho = 58 THEN 32 ELSE 48 END,
    850, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, g.gene_ticket_pie, 1, 4, 1, 0, 1
FROM dbo.Generales g ORDER BY g.id_gene;");

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

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Impresion_Cola");

            migrationBuilder.DropTable(
                name: "Ticketera");

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
        }
    }
}
