using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalExamination.Migrations
{
    /// <inheritdoc />
    public partial class AddDboDoctor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndShift",
                table: "DboDoctors",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartShift",
                table: "DboDoctors",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Surname",
                table: "DboDoctors",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndShift",
                table: "DboDoctors");

            migrationBuilder.DropColumn(
                name: "StartShift",
                table: "DboDoctors");

            migrationBuilder.DropColumn(
                name: "Surname",
                table: "DboDoctors");
        }
    }
}
