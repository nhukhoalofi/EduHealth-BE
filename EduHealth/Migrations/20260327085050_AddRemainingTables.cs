using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduHealth.Migrations
{
    /// <inheritdoc />
    public partial class AddRemainingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HealthVisits",
                columns: table => new
                {
                    VisitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NurseId = table.Column<int>(type: "int", nullable: false),
                    VisitDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Symptoms = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Diagnosis = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MeasuredHeight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MeasuredWeight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DiseaseId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthVisits", x => x.VisitId);
                    table.ForeignKey(
                        name: "FK_HealthVisits_DiseaseType_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "DiseaseType",
                        principalColumn: "DiseaseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HealthVisits_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HealthVisits_Users_NurseId",
                        column: x => x.NurseId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MedicineStockLogs",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicineId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ChangeQty = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineStockLogs", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_MedicineStockLogs_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicines",
                        principalColumn: "MedicineId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicineStockLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentAllergies",
                columns: table => new
                {
                    RecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AllergyId = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAllergies", x => x.RecordId);
                    table.ForeignKey(
                        name: "FK_StudentAllergies_AllergyTypes_AllergyId",
                        column: x => x.AllergyId,
                        principalTable: "AllergyTypes",
                        principalColumn: "AllergyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentAllergies_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentVaccinations",
                columns: table => new
                {
                    RecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VaccinationId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentVaccinations", x => x.RecordId);
                    table.ForeignKey(
                        name: "FK_StudentVaccinations_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentVaccinations_Vaccinations_VaccinationId",
                        column: x => x.VaccinationId,
                        principalTable: "Vaccinations",
                        principalColumn: "VaccinationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SystemAlerts",
                columns: table => new
                {
                    AlertId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlertType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemAlerts", x => x.AlertId);
                });

            migrationBuilder.CreateTable(
                name: "VisitPrescriptions",
                columns: table => new
                {
                    PrescriptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitId = table.Column<int>(type: "int", nullable: false),
                    MedicineId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitPrescriptions", x => x.PrescriptionId);
                    table.ForeignKey(
                        name: "FK_VisitPrescriptions_HealthVisits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "HealthVisits",
                        principalColumn: "VisitId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VisitPrescriptions_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicines",
                        principalColumn: "MedicineId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HealthVisits_DiseaseId",
                table: "HealthVisits",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthVisits_NurseId",
                table: "HealthVisits",
                column: "NurseId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthVisits_StudentId",
                table: "HealthVisits",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineStockLogs_MedicineId",
                table: "MedicineStockLogs",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineStockLogs_UserId",
                table: "MedicineStockLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAllergies_AllergyId",
                table: "StudentAllergies",
                column: "AllergyId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAllergies_StudentId_AllergyId",
                table: "StudentAllergies",
                columns: new[] { "StudentId", "AllergyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentVaccinations_StudentId_VaccinationId",
                table: "StudentVaccinations",
                columns: new[] { "StudentId", "VaccinationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentVaccinations_VaccinationId",
                table: "StudentVaccinations",
                column: "VaccinationId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitPrescriptions_MedicineId",
                table: "VisitPrescriptions",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitPrescriptions_VisitId",
                table: "VisitPrescriptions",
                column: "VisitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicineStockLogs");

            migrationBuilder.DropTable(
                name: "StudentAllergies");

            migrationBuilder.DropTable(
                name: "StudentVaccinations");

            migrationBuilder.DropTable(
                name: "SystemAlerts");

            migrationBuilder.DropTable(
                name: "VisitPrescriptions");

            migrationBuilder.DropTable(
                name: "HealthVisits");
        }
    }
}
