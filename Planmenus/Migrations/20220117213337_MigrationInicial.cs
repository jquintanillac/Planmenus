using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planmenus.Migrations
{
    public partial class MigrationInicial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Caja",
                columns: table => new
                {
                    id_caja = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_menu = table.Column<int>(type: "int", nullable: false),
                    caja_desc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    caja_monto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    caja_fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Caja", x => x.id_caja);
                });

            migrationBuilder.CreateTable(
                name: "Menu",
                columns: table => new
                {
                    id_menu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    menu_desc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    menu_precio = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    menu_observ = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    menu_fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menu", x => x.id_menu);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Usua_user = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Usua_nombres = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Usua_apellidos = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Usua_email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Usua_pass = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Usua_Hash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Usua_act = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.IdUsuario);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Caja");

            migrationBuilder.DropTable(
                name: "Menu");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
