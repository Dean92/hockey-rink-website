using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HockeyRinkAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddNullableLeagueIdToSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Leagues_LeagueId",
                table: "Sessions");

            migrationBuilder.AlterColumn<int>(
                name: "LeagueId",
                table: "Sessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Leagues_LeagueId",
                table: "Sessions",
                column: "LeagueId",
                principalTable: "Leagues",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Leagues_LeagueId",
                table: "Sessions");

            migrationBuilder.AlterColumn<int>(
                name: "LeagueId",
                table: "Sessions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Leagues_LeagueId",
                table: "Sessions",
                column: "LeagueId",
                principalTable: "Leagues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
