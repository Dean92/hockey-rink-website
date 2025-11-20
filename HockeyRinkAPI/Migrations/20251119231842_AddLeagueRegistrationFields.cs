using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HockeyRinkAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddLeagueRegistrationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EarlyBirdPrice",
                table: "Leagues",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrationCloseDate",
                table: "Leagues",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrationOpenDate",
                table: "Leagues",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Leagues",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EarlyBirdPrice",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "RegistrationCloseDate",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "RegistrationOpenDate",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Leagues");
        }
    }
}
