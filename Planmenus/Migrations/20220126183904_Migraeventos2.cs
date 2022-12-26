using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planmenus.Migrations
{
    public partial class Migraeventos2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "event_title",
                table: "Eventos",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "event_local",
                table: "Eventos",
                newName: "Location");

            migrationBuilder.RenameColumn(
                name: "event_fecini",
                table: "Eventos",
                newName: "StarDate");

            migrationBuilder.RenameColumn(
                name: "event_fecfin",
                table: "Eventos",
                newName: "EndDate");

            migrationBuilder.RenameColumn(
                name: "event_descrp",
                table: "Eventos",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "id_event",
                table: "Eventos",
                newName: "id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "title",
                table: "Eventos",
                newName: "event_title");

            migrationBuilder.RenameColumn(
                name: "StarDate",
                table: "Eventos",
                newName: "event_fecini");

            migrationBuilder.RenameColumn(
                name: "Location",
                table: "Eventos",
                newName: "event_local");

            migrationBuilder.RenameColumn(
                name: "EndDate",
                table: "Eventos",
                newName: "event_fecfin");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Eventos",
                newName: "event_descrp");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Eventos",
                newName: "id_event");
        }
    }
}
