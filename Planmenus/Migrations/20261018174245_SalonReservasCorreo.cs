using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planmenus.Migrations
{
    /// <inheritdoc />
    public partial class SalonReservasCorreo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_zona",
                table: "Mesas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "mesa_alto",
                table: "Mesas",
                type: "int",
                nullable: false,
                defaultValue: 90);

            migrationBuilder.AddColumn<int>(
                name: "mesa_ancho",
                table: "Mesas",
                type: "int",
                nullable: false,
                defaultValue: 90);

            migrationBuilder.AddColumn<int>(
                name: "mesa_capacidad",
                table: "Mesas",
                type: "int",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AddColumn<string>(
                name: "mesa_forma",
                table: "Mesas",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "mesa_rotacion",
                table: "Mesas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "mesa_x",
                table: "Mesas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "mesa_y",
                table: "Mesas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gene_correo_avisos",
                table: "Generales",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "gene_recordatorio_horas",
                table: "Generales",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "gene_reserva_minutos",
                table: "Generales",
                type: "int",
                nullable: false,
                defaultValue: 120);

            migrationBuilder.AddColumn<string>(
                name: "caja_mozo",
                table: "Caja",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Asignacion_Mozo",
                columns: table => new
                {
                    id_asig = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    asig_fecha = table.Column<DateTime>(type: "date", nullable: false),
                    id_mesa = table.Column<int>(type: "int", nullable: false),
                    IdUsuario = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asignacion_Mozo", x => x.id_asig);
                    table.ForeignKey(
                        name: "FK_Asignacion_Mozo_Mesas_id_mesa",
                        column: x => x.id_mesa,
                        principalTable: "Mesas",
                        principalColumn: "id_mesa",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Asignacion_Mozo_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Correo_Log",
                columns: table => new
                {
                    id_correo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    cor_fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    cor_tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    id_reserva = table.Column<int>(type: "int", nullable: true),
                    cor_para = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    cor_asunto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    cor_ok = table.Column<bool>(type: "bit", nullable: false),
                    cor_error = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Correo_Log", x => x.id_correo);
                });

            migrationBuilder.CreateTable(
                name: "Reserva",
                columns: table => new
                {
                    id_reserva = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    res_cliente = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    res_telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    res_email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    res_emails_extra = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    res_inicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    res_fin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    res_personas = table.Column<int>(type: "int", nullable: false),
                    res_tipo = table.Column<byte>(type: "tinyint", nullable: false),
                    res_estado = table.Column<byte>(type: "tinyint", nullable: false),
                    res_notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    res_usuario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    res_creado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    res_confirmacion_enviada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    res_recordatorio_enviado = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reserva", x => x.id_reserva);
                });

            migrationBuilder.CreateTable(
                name: "Zona",
                columns: table => new
                {
                    id_zona = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    zona_desc = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    zona_orden = table.Column<int>(type: "int", nullable: false),
                    zona_act = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zona", x => x.id_zona);
                });

            migrationBuilder.CreateTable(
                name: "Reserva_Mesa",
                columns: table => new
                {
                    id_resmesa = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_reserva = table.Column<int>(type: "int", nullable: false),
                    id_mesa = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reserva_Mesa", x => x.id_resmesa);
                    table.ForeignKey(
                        name: "FK_Reserva_Mesa_Mesas_id_mesa",
                        column: x => x.id_mesa,
                        principalTable: "Mesas",
                        principalColumn: "id_mesa",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reserva_Mesa_Reserva_id_reserva",
                        column: x => x.id_reserva,
                        principalTable: "Reserva",
                        principalColumn: "id_reserva",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mesas_id_zona",
                table: "Mesas",
                column: "id_zona");

            migrationBuilder.CreateIndex(
                name: "IX_Asignacion_Mozo_asig_fecha_id_mesa",
                table: "Asignacion_Mozo",
                columns: new[] { "asig_fecha", "id_mesa" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Asignacion_Mozo_id_mesa",
                table: "Asignacion_Mozo",
                column: "id_mesa");

            migrationBuilder.CreateIndex(
                name: "IX_Asignacion_Mozo_IdUsuario",
                table: "Asignacion_Mozo",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Correo_Log_cor_fecha",
                table: "Correo_Log",
                column: "cor_fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Reserva_res_inicio_res_estado",
                table: "Reserva",
                columns: new[] { "res_inicio", "res_estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Reserva_Mesa_id_mesa",
                table: "Reserva_Mesa",
                column: "id_mesa");

            migrationBuilder.CreateIndex(
                name: "IX_Reserva_Mesa_id_reserva_id_mesa",
                table: "Reserva_Mesa",
                columns: new[] { "id_reserva", "id_mesa" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Mesas_Zona_id_zona",
                table: "Mesas",
                column: "id_zona",
                principalTable: "Zona",
                principalColumn: "id_zona",
                onDelete: ReferentialAction.SetNull);

            // Zona inicial con las mesas existentes y forma por defecto
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM dbo.Zona)
    INSERT INTO dbo.Zona (zona_desc, zona_orden, zona_act) VALUES (N'Salón', 1, 1);
UPDATE dbo.Mesas SET id_zona = (SELECT MIN(id_zona) FROM dbo.Zona) WHERE id_zona IS NULL;
UPDATE dbo.Mesas SET mesa_forma = N'cuadrada' WHERE mesa_forma IS NULL;");

            // Submenus: Reservas y Planificar mozos (Caja/Atencion), Plano del salon (Planificacion). Activos solo para el Administrador.
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT dbo.Submenu ON;
IF NOT EXISTS (SELECT 1 FROM dbo.Submenu WHERE id_submenu = 28)
    INSERT INTO dbo.Submenu (id_submenu, id_menprin, submenu_desc, submenu_nro) VALUES (28, 3, N'Reservas', N'11');
IF NOT EXISTS (SELECT 1 FROM dbo.Submenu WHERE id_submenu = 29)
    INSERT INTO dbo.Submenu (id_submenu, id_menprin, submenu_desc, submenu_nro) VALUES (29, 3, N'Planificar mozos', N'12');
IF NOT EXISTS (SELECT 1 FROM dbo.Submenu WHERE id_submenu = 30)
    INSERT INTO dbo.Submenu (id_submenu, id_menprin, submenu_desc, submenu_nro) VALUES (30, 2, N'Plano del salón', N'10');
SET IDENTITY_INSERT dbo.Submenu OFF;
INSERT INTO dbo.Rol_Permisos (id_rol, id_menprin, id_submenu, rolper_act)
SELECT r.id_rol, s.id_menprin, s.id_submenu, CASE WHEN r.id_rol = 1 THEN 1 ELSE 0 END
FROM dbo.Rol r CROSS JOIN dbo.Submenu s
WHERE s.id_submenu IN (28, 29, 30)
  AND NOT EXISTS (SELECT 1 FROM dbo.Rol_Permisos p WHERE p.id_rol = r.id_rol AND p.id_submenu = s.id_submenu);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mesas_Zona_id_zona",
                table: "Mesas");

            migrationBuilder.DropTable(
                name: "Asignacion_Mozo");

            migrationBuilder.DropTable(
                name: "Correo_Log");

            migrationBuilder.DropTable(
                name: "Reserva_Mesa");

            migrationBuilder.DropTable(
                name: "Zona");

            migrationBuilder.DropTable(
                name: "Reserva");

            migrationBuilder.DropIndex(
                name: "IX_Mesas_id_zona",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "id_zona",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "mesa_alto",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "mesa_ancho",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "mesa_capacidad",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "mesa_forma",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "mesa_rotacion",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "mesa_x",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "mesa_y",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "gene_correo_avisos",
                table: "Generales");

            migrationBuilder.DropColumn(
                name: "gene_recordatorio_horas",
                table: "Generales");

            migrationBuilder.DropColumn(
                name: "gene_reserva_minutos",
                table: "Generales");

            migrationBuilder.DropColumn(
                name: "caja_mozo",
                table: "Caja");
        }
    }
}
