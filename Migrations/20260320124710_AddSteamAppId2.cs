using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balanced_Gaming.Migrations
{
    /// <inheritdoc />
    public partial class AddSteamAppId2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "steamAppId",
                table: "games",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "steamAppId",
                table: "games");
        }
    }
}
