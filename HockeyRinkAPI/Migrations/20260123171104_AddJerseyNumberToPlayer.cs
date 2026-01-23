using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HockeyRinkAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddJerseyNumberToPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "JerseyNumber",
                table: "Players",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JerseyNumber",
                table: "Players");
        }
    }
}
