using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HockeyRinkAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddRinkSchedulerSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Leagues_LeagueId",
                table: "Teams");

            migrationBuilder.AddColumn<int>(
                name: "RinkId",
                table: "Sessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RinkId",
                table: "Games",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Rinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RinkBlockouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RinkId = table.Column<int>(type: "int", nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RinkBlockouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RinkBlockouts_Rinks_RinkId",
                        column: x => x.RinkId,
                        principalTable: "Rinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_RinkId",
                table: "Sessions",
                column: "RinkId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_RinkId_GameDate",
                table: "Games",
                columns: new[] { "RinkId", "GameDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RinkBlockouts_RinkId_StartDateTime",
                table: "RinkBlockouts",
                columns: new[] { "RinkId", "StartDateTime" });

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Rinks_RinkId",
                table: "Games",
                column: "RinkId",
                principalTable: "Rinks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Rinks_RinkId",
                table: "Sessions",
                column: "RinkId",
                principalTable: "Rinks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Leagues_LeagueId",
                table: "Teams",
                column: "LeagueId",
                principalTable: "Leagues",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Rinks_RinkId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Rinks_RinkId",
                table: "Sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Leagues_LeagueId",
                table: "Teams");

            migrationBuilder.DropTable(
                name: "RinkBlockouts");

            migrationBuilder.DropTable(
                name: "Rinks");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_RinkId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Games_RinkId_GameDate",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "RinkId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "RinkId",
                table: "Games");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Leagues_LeagueId",
                table: "Teams",
                column: "LeagueId",
                principalTable: "Leagues",
                principalColumn: "Id");
        }
    }
}
