using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planmenus.Migrations
{
    /// <inheritdoc />
    public partial class PagoMercadoPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "pago_mp_id",
                table: "Pago_Suscripcion",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pago_Suscripcion_pago_mp_id",
                table: "Pago_Suscripcion",
                column: "pago_mp_id",
                unique: true,
                filter: "[pago_mp_id] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pago_Suscripcion_pago_mp_id",
                table: "Pago_Suscripcion");

            migrationBuilder.DropColumn(
                name: "pago_mp_id",
                table: "Pago_Suscripcion");
        }
    }
}
