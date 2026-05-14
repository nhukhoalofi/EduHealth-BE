using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduHealth.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationVisibilityStatusPublishedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "Notifications",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Notifications",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PUBLISHED");

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                table: "Notifications",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "INTERNAL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_Status",
                table: "Notifications",
                sql: "[Status] IN ('DRAFT', 'PUBLISHED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_Visibility",
                table: "Notifications",
                sql: "[Visibility] IN ('INTERNAL', 'PUBLIC', 'BOTH')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_Status",
                table: "Notifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_Visibility",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Notifications");
        }
    }
}
