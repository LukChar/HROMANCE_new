using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRomance.Migrations
{
    /// <inheritdoc />
    public partial class AddAuftragQualifikationen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuftragQualifikation",
                columns: table => new
                {
                    AuftraegeId = table.Column<int>(type: "INTEGER", nullable: false),
                    QualifikationenId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuftragQualifikation", x => new { x.AuftraegeId, x.QualifikationenId });
                    table.ForeignKey(
                        name: "FK_AuftragQualifikation_Auftraege_AuftraegeId",
                        column: x => x.AuftraegeId,
                        principalTable: "Auftraege",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuftragQualifikation_Qualifikationen_QualifikationenId",
                        column: x => x.QualifikationenId,
                        principalTable: "Qualifikationen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuftragQualifikation_QualifikationenId",
                table: "AuftragQualifikation",
                column: "QualifikationenId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuftragQualifikation");
        }
    }
}
