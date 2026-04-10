using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduHealth.Migrations
{
    /// <inheritdoc />
    public partial class AddVaccinationCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentVaccinations_UserId_VaccinationId",
                table: "StudentVaccinations");

            migrationBuilder.AddColumn<int>(
                name: "CampaignId",
                table: "StudentVaccinations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LotNumber",
                table: "StudentVaccinations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "StudentVaccinations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "StudentVaccinations",
                type: "datetime",
                nullable: false,
                defaultValue: new DateTime(1753, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateOnly>(
                name: "VaccinatedAt",
                table: "StudentVaccinations",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VaccinationCampaigns",
                columns: table => new
                {
                    CampaignId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    VaccineName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DoseNumber = table.Column<int>(type: "int", nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaccinationCampaigns", x => x.CampaignId);
                    table.ForeignKey(
                        name: "FK_VaccinationCampaigns_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VaccinationCampaignTargetClasses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignId = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaccinationCampaignTargetClasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaccinationCampaignTargetClasses_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "ClassId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VaccinationCampaignTargetClasses_VaccinationCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "VaccinationCampaigns",
                        principalColumn: "CampaignId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(@"
DECLARE @CreatedByUserId int = (SELECT TOP 1 [UserId] FROM [Users] ORDER BY [UserId]);

INSERT INTO [VaccinationCampaigns]
    ([Code], [Name], [VaccineName], [DoseNumber], [ScheduledDate], [TargetType], [Status], [Note], [CreatedByUserId], [CreatedAt])
SELECT
    CONCAT('VACLEG', RIGHT('000' + CAST(sv.[VaccinationId] AS varchar(3)), 3)) AS [Code],
    CONCAT('Legacy campaign - ', ISNULL(v.[Name], CONCAT('Vaccine ', sv.[VaccinationId]))) AS [Name],
    ISNULL(v.[Name], CONCAT('Vaccine ', sv.[VaccinationId])) AS [VaccineName],
    1 AS [DoseNumber],
    CAST(GETDATE() AS date) AS [ScheduledDate],
    'STUDENT' AS [TargetType],
    'COMPLETED' AS [Status],
    'Auto-created during migration for legacy student vaccinations.' AS [Note],
    ISNULL(@CreatedByUserId, 1) AS [CreatedByUserId],
    GETDATE() AS [CreatedAt]
FROM (
    SELECT DISTINCT [VaccinationId]
    FROM [StudentVaccinations]
) sv
LEFT JOIN [Vaccinations] v ON v.[VaccinationId] = sv.[VaccinationId]
WHERE NOT EXISTS (
    SELECT 1
    FROM [VaccinationCampaigns] c
    WHERE c.[Code] = CONCAT('VACLEG', RIGHT('000' + CAST(sv.[VaccinationId] AS varchar(3)), 3))
);

UPDATE sv
SET
    sv.[CampaignId] = c.[CampaignId],
    sv.[UpdatedAt] = CASE WHEN sv.[UpdatedAt] IS NULL OR sv.[UpdatedAt] < '1753-01-01' THEN GETDATE() ELSE sv.[UpdatedAt] END
FROM [StudentVaccinations] sv
INNER JOIN [VaccinationCampaigns] c
    ON c.[Code] = CONCAT('VACLEG', RIGHT('000' + CAST(sv.[VaccinationId] AS varchar(3)), 3))
WHERE sv.[CampaignId] = 0;
");

            migrationBuilder.CreateIndex(
                name: "IX_StudentVaccinations_CampaignId_UserId",
                table: "StudentVaccinations",
                columns: new[] { "CampaignId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentVaccinations_UserId",
                table: "StudentVaccinations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationCampaigns_Code",
                table: "VaccinationCampaigns",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationCampaigns_CreatedByUserId",
                table: "VaccinationCampaigns",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationCampaignTargetClasses_CampaignId_ClassId",
                table: "VaccinationCampaignTargetClasses",
                columns: new[] { "CampaignId", "ClassId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationCampaignTargetClasses_ClassId",
                table: "VaccinationCampaignTargetClasses",
                column: "ClassId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentVaccinations_VaccinationCampaigns_CampaignId",
                table: "StudentVaccinations",
                column: "CampaignId",
                principalTable: "VaccinationCampaigns",
                principalColumn: "CampaignId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentVaccinations_VaccinationCampaigns_CampaignId",
                table: "StudentVaccinations");

            migrationBuilder.DropTable(
                name: "VaccinationCampaignTargetClasses");

            migrationBuilder.DropTable(
                name: "VaccinationCampaigns");

            migrationBuilder.DropIndex(
                name: "IX_StudentVaccinations_CampaignId_UserId",
                table: "StudentVaccinations");

            migrationBuilder.DropIndex(
                name: "IX_StudentVaccinations_UserId",
                table: "StudentVaccinations");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "StudentVaccinations");

            migrationBuilder.DropColumn(
                name: "LotNumber",
                table: "StudentVaccinations");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "StudentVaccinations");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "StudentVaccinations");

            migrationBuilder.DropColumn(
                name: "VaccinatedAt",
                table: "StudentVaccinations");

            migrationBuilder.CreateIndex(
                name: "IX_StudentVaccinations_UserId_VaccinationId",
                table: "StudentVaccinations",
                columns: new[] { "UserId", "VaccinationId" },
                unique: true);
        }
    }
}
