using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planmenus.Migrations
{
    public partial class Migraeventos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Eventos",
                columns: table => new
                {
                    id_event = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    event_title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    event_descrp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    event_fecini = table.Column<DateTime>(type: "datetime2", nullable: false),
                    event_fecfin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    event_local = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eventos", x => x.id_event);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Eventos");
        }
    }
}
