using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRomance.Migrations
{
    /// <inheritdoc />
    public partial class AddArbeitszeitUndAbwesenheit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Abwesenheiten",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MitarbeiterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Von = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Bis = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Typ = table.Column<string>(type: "TEXT", nullable: false),
                    Grund = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Abwesenheiten", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Abwesenheiten_Mitarbeiter_MitarbeiterId",
                        column: x => x.MitarbeiterId,
                        principalTable: "Mitarbeiter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Arbeitszeiten",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MitarbeiterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Beginn = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    Ende = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    PauseMinuten = table.Column<int>(type: "INTEGER", nullable: false),
                    Notiz = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Arbeitszeiten", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Arbeitszeiten_Mitarbeiter_MitarbeiterId",
                        column: x => x.MitarbeiterId,
                        principalTable: "Mitarbeiter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Abwesenheiten_MitarbeiterId",
                table: "Abwesenheiten",
                column: "MitarbeiterId");

            migrationBuilder.CreateIndex(
                name: "IX_Arbeitszeiten_MitarbeiterId",
                table: "Arbeitszeiten",
                column: "MitarbeiterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Abwesenheiten");

            migrationBuilder.DropTable(
                name: "Arbeitszeiten");

        }
    }
}
