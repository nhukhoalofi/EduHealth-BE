using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduHealth.Migrations
{
    /// <inheritdoc />
    public partial class EnsureNearestExpiryDateColumnInMedicines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "NearestExpiryDate",
                table: "Medicines",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NearestExpiryDate",
                table: "Medicines");
        }
    }
}
