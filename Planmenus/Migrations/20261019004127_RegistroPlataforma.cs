using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planmenus.Migrations
{
    /// <inheritdoc />
    public partial class RegistroPlataforma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Usua_superadmin",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "Usua_ultimo_ingreso",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);

            // El administrador de la empresa original es el dueño de la plataforma
            migrationBuilder.Sql("UPDATE Usuarios SET Usua_superadmin = 1 WHERE Usua_user = 'admin' AND id_empresa = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Usua_superadmin",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Usua_ultimo_ingreso",
                table: "Usuarios");
        }
    }
}
