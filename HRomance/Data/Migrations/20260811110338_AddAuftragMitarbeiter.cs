using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRomance.Migrations
{
    /// <inheritdoc />
    public partial class AddAuftragMitarbeiter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuftragMitarbeiter",
                columns: table => new
                {
                    AuftraegeId = table.Column<int>(type: "INTEGER", nullable: false),
                    MitarbeiterId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuftragMitarbeiter", x => new { x.AuftraegeId, x.MitarbeiterId });
                    table.ForeignKey(
                        name: "FK_AuftragMitarbeiter_Auftraege_AuftraegeId",
                        column: x => x.AuftraegeId,
                        principalTable: "Auftraege",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuftragMitarbeiter_Mitarbeiter_MitarbeiterId",
                        column: x => x.MitarbeiterId,
                        principalTable: "Mitarbeiter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuftragMitarbeiter_MitarbeiterId",
                table: "AuftragMitarbeiter",
                column: "MitarbeiterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuftragMitarbeiter");

        }
    }
}
