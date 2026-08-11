using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRomance.Migrations
{
    /// <inheritdoc />
    public partial class AddMitarbeiterQualifikationen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MitarbeiterQualifikation",
                columns: table => new
                {
                    MitarbeiterId = table.Column<int>(type: "INTEGER", nullable: false),
                    QualifikationenId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MitarbeiterQualifikation", x => new { x.MitarbeiterId, x.QualifikationenId });
                    table.ForeignKey(
                        name: "FK_MitarbeiterQualifikation_Mitarbeiter_MitarbeiterId",
                        column: x => x.MitarbeiterId,
                        principalTable: "Mitarbeiter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MitarbeiterQualifikation_Qualifikationen_QualifikationenId",
                        column: x => x.QualifikationenId,
                        principalTable: "Qualifikationen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MitarbeiterQualifikation_QualifikationenId",
                table: "MitarbeiterQualifikation",
                column: "QualifikationenId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MitarbeiterQualifikation");
        }
    }
}
