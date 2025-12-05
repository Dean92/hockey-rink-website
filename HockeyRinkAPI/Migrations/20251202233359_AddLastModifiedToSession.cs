using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HockeyRinkAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddLastModifiedToSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "Sessions",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "Sessions");
        }
    }
}
