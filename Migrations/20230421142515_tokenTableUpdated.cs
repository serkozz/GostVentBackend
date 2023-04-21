using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GostVentBackend.Migrations
{
    /// <inheritdoc />
    public partial class tokenTableUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Expires",
                table: "Token",
                newName: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                table: "Token",
                newName: "Expires");
        }
    }
}
