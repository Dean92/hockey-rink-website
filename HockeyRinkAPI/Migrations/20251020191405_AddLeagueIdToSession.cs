using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HockeyRinkAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddLeagueIdToSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Sessions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true
            );

            // First add the column as nullable
            migrationBuilder.AddColumn<int>(
                name: "LeagueId",
                table: "Sessions",
                type: "int",
                nullable: true
            );

            // Update existing sessions to use the first available league
            migrationBuilder.Sql(
                @"
                UPDATE Sessions 
                SET LeagueId = (SELECT TOP 1 Id FROM Leagues ORDER BY Id)
                WHERE LeagueId IS NULL;
            "
            );

            // Make the column non-nullable
            migrationBuilder.AlterColumn<int>(
                name: "LeagueId",
                table: "Sessions",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_LeagueId",
                table: "Sessions",
                column: "LeagueId"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Leagues_LeagueId",
                table: "Sessions",
                column: "LeagueId",
                principalTable: "Leagues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Leagues_LeagueId",
                table: "Sessions"
            );

            migrationBuilder.DropIndex(name: "IX_Sessions_LeagueId", table: "Sessions");

            migrationBuilder.DropColumn(name: "LeagueId", table: "Sessions");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100
            );
        }
    }
}
