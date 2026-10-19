using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planmenus.Migrations
{
    /// <inheritdoc />
    public partial class AvisosSuscripcion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "pago_comprobante",
                table: "Pago_Suscripcion",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pago_nota",
                table: "Pago_Suscripcion",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "pago_procesado",
                table: "Pago_Suscripcion",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Los pagos anteriores no reciben comprobante ni recibo de forma retroactiva
            migrationBuilder.Sql("UPDATE Pago_Suscripcion SET pago_procesado = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pago_comprobante",
                table: "Pago_Suscripcion");

            migrationBuilder.DropColumn(
                name: "pago_nota",
                table: "Pago_Suscripcion");

            migrationBuilder.DropColumn(
                name: "pago_procesado",
                table: "Pago_Suscripcion");
        }
    }
}
