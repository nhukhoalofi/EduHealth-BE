using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduHealth.Migrations
{
    /// <inheritdoc />
    public partial class SyncNearestExpiryDateSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF COL_LENGTH('Medicines', 'NearestExpiryDate') IS NULL ALTER TABLE [Medicines] ADD [NearestExpiryDate] date NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF COL_LENGTH('Medicines', 'NearestExpiryDate') IS NOT NULL ALTER TABLE [Medicines] DROP COLUMN [NearestExpiryDate];");
        }
    }
}
