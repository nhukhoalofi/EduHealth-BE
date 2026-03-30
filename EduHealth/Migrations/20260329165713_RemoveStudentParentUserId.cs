using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduHealth.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStudentParentUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Users_ParentUserId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_ParentUserId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "ParentUserId",
                table: "Students");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentUserId",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Students_ParentUserId",
                table: "Students",
                column: "ParentUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Users_ParentUserId",
                table: "Students",
                column: "ParentUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
