using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HockeyRinkAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionRegistrationDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "SessionRegistrations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaid",
                table: "SessionRegistrations",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "SessionRegistrations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "SessionRegistrations",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "SessionRegistrations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "SessionRegistrations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "SessionRegistrations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Position",
                table: "SessionRegistrations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrationDate",
                table: "SessionRegistrations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "SessionRegistrations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                table: "SessionRegistrations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionRegistrations_RegistrationDate",
                table: "SessionRegistrations",
                column: "RegistrationDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SessionRegistrations_RegistrationDate",
                table: "SessionRegistrations");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "SessionRegistrations");

            migrationBuilder.DropColumn(
                name: "AmountPaid",
                table: "SessionRegistrations");

            migrationBuilder.DropColumn(
                name: "City",
                table: "SessionRegistrations");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "SessionRegistrations");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "SessionRegistrations");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "SessionRegistrations");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "SessionRegistrations");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "SessionRegistrations");

            migrationBuilder.DropColumn(
                name: "RegistrationDate",
                table: "SessionRegistrations");

            migrationBuilder.DropColumn(
                name: "State",
                table: "SessionRegistrations");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                table: "SessionRegistrations");
        }
    }
}
