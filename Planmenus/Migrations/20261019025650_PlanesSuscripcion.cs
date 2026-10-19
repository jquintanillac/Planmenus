using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planmenus.Migrations
{
    /// <inheritdoc />
    public partial class PlanesSuscripcion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "emp_vence",
                table: "Empresa",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Pago_Suscripcion",
                columns: table => new
                {
                    id_pago = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_empresa = table.Column<int>(type: "int", nullable: false),
                    pago_fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    pago_monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    pago_meses = table.Column<int>(type: "int", nullable: false),
                    pago_plan = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    pago_medio = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    pago_referencia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    pago_vence = table.Column<DateTime>(type: "datetime2", nullable: false),
                    pago_usuario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pago_Suscripcion", x => x.id_pago);
                    table.ForeignKey(
                        name: "FK_Pago_Suscripcion_Empresa_id_empresa",
                        column: x => x.id_empresa,
                        principalTable: "Empresa",
                        principalColumn: "id_empresa",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pago_Suscripcion_id_empresa",
                table: "Pago_Suscripcion",
                column: "id_empresa");

            // La empresa de la plataforma no vence; las demas que ya existan empiezan su periodo de prueba
            migrationBuilder.Sql("UPDATE Empresa SET emp_vence = DATEADD(day, 14, CAST(GETDATE() AS date)) WHERE id_empresa <> 1");
            migrationBuilder.Sql("UPDATE Empresa SET emp_plan = 'premium' WHERE id_empresa = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pago_Suscripcion");

            migrationBuilder.DropColumn(
                name: "emp_vence",
                table: "Empresa");
        }
    }
}
