using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HockeyRinkAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionGameTypeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RegularSeasonGames",
                table: "Sessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GameType",
                table: "Games",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegularSeasonGames",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "GameType",
                table: "Games");
        }
    }
}
