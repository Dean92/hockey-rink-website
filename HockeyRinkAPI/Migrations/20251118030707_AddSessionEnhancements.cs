using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HockeyRinkAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EarlyBirdEndDate",
                table: "Sessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EarlyBirdPrice",
                table: "Sessions",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxPlayers",
                table: "Sessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrationCloseDate",
                table: "Sessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrationOpenDate",
                table: "Sessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RegularPrice",
                table: "Sessions",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EarlyBirdEndDate",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "EarlyBirdPrice",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "MaxPlayers",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "RegistrationCloseDate",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "RegistrationOpenDate",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "RegularPrice",
                table: "Sessions");
        }
    }
}
