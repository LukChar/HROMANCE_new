using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRomance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationUserMitarbeiter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MitarbeiterId",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_MitarbeiterId",
                table: "AspNetUsers",
                column: "MitarbeiterId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Mitarbeiter_MitarbeiterId",
                table: "AspNetUsers",
                column: "MitarbeiterId",
                principalTable: "Mitarbeiter",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Mitarbeiter_MitarbeiterId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_MitarbeiterId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MitarbeiterId",
                table: "AspNetUsers");
        }
    }
}
