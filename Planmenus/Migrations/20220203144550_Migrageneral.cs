using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planmenus.Migrations
{
    public partial class Migrageneral : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Generales",
                columns: table => new
                {
                    id_gene = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    gene_empre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    gene_ruc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    gene_direc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    gene_telef = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    gene_url = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Generales", x => x.id_gene);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Generales");
        }
    }
}
