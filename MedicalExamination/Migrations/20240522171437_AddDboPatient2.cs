using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalExamination.Migrations
{
    /// <inheritdoc />
    public partial class AddDboPatient2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Surname",
                table: "DboPatients",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Surname",
                table: "DboPatients");
        }
    }
}
