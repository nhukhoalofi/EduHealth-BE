using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduHealth.Migrations
{
    /// <inheritdoc />
    public partial class AddVaccinationDescriptionAndCreatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Vaccinations",
                type: "datetime",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Vaccinations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Vaccinations");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Vaccinations");
        }
    }
}
