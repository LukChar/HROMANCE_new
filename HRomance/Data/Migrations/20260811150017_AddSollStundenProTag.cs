using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRomance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSollStundenProTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "SollStundenProTag",
                table: "Mitarbeiter",
                type: "REAL",
                nullable: false,
                defaultValue: 8.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SollStundenProTag",
                table: "Mitarbeiter");
        }
    }
}
