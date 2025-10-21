using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HockeyRinkAPI.Migrations
{
    /// <inheritdoc />
    public partial class EnforceLeagueIdConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, update any NULL LeagueId values to a valid league
            migrationBuilder.Sql(
                @"
                UPDATE Sessions 
                SET LeagueId = (SELECT TOP 1 Id FROM Leagues ORDER BY Id)
                WHERE LeagueId IS NULL AND EXISTS(SELECT 1 FROM Leagues);
            "
            );

            // Drop the existing foreign key if it exists
            migrationBuilder.Sql(
                @"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Sessions_Leagues_LeagueId')
                    ALTER TABLE Sessions DROP CONSTRAINT FK_Sessions_Leagues_LeagueId;
            "
            );

            // Make LeagueId non-nullable
            migrationBuilder.AlterColumn<int>(
                name: "LeagueId",
                table: "Sessions",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true
            );

            // Add the foreign key constraint with CASCADE delete
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

            migrationBuilder.AlterColumn<int>(
                name: "LeagueId",
                table: "Sessions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: false
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Leagues_LeagueId",
                table: "Sessions",
                column: "LeagueId",
                principalTable: "Leagues",
                principalColumn: "Id"
            );
        }
    }
}
