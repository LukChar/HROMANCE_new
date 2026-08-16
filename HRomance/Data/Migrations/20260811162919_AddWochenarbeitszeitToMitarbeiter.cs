using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRomance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWochenarbeitszeitToMitarbeiter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Wochenarbeitszeit",
                table: "Mitarbeiter",
                type: "REAL",
                nullable: false,
                defaultValue: 40.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Wochenarbeitszeit",
                table: "Mitarbeiter");
        }
    }
}
