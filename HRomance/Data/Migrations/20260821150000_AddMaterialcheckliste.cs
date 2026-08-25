using HRomance.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HRomance.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260821150000_AddMaterialcheckliste")]
public class AddMaterialcheckliste : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Materialeintraege",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Bezeichnung = table.Column<string>(type: "TEXT", nullable: false),
                Anzahl = table.Column<int>(type: "INTEGER", nullable: false),
                Erledigt = table.Column<bool>(type: "INTEGER", nullable: false),
                AuftragId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Materialeintraege", x => x.Id);
                table.ForeignKey(
                    name: "FK_Materialeintraege_Auftraege_AuftragId",
                    column: x => x.AuftragId,
                    principalTable: "Auftraege",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Materialeintraege_AuftragId",
            table: "Materialeintraege",
            column: "AuftragId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Materialeintraege");
    }
}
