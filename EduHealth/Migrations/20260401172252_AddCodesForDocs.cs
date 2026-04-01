using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduHealth.Migrations
{
    /// <inheritdoc />
    public partial class AddCodesForDocs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Students",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "HealthVisits",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "HealthVisits",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Treatment",
                table: "HealthVisits",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "DiseaseType",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Classes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_HealthVisits_Code",
                table: "HealthVisits",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_Code",
                table: "Students",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseType_Code",
                table: "DiseaseType",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classes_Code",
                table: "Classes",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HealthVisits_Code",
                table: "HealthVisits");

            migrationBuilder.DropIndex(
                name: "IX_Classes_Code",
                table: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_Students_Code",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_DiseaseType_Code",
                table: "DiseaseType");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "HealthVisits");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "HealthVisits");

            migrationBuilder.DropColumn(
                name: "Treatment",
                table: "HealthVisits");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "DiseaseType");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Classes");
        }
    }
}
