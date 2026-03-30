using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduHealth.Migrations
{
    /// <inheritdoc />
    public partial class AlignSchemaToErd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HealthVisits_Students_StudentId",
                table: "HealthVisits");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentAllergies_Students_StudentId",
                table: "StudentAllergies");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentVaccinations_Students_StudentId",
                table: "StudentVaccinations");

            migrationBuilder.DropIndex(
                name: "IX_StudentVaccinations_StudentId_VaccinationId",
                table: "StudentVaccinations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Students",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_StudentAllergies_StudentId_AllergyId",
                table: "StudentAllergies");

            migrationBuilder.DropIndex(
                name: "IX_HealthVisits_StudentId",
                table: "HealthVisits");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "StudentVaccinations");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "StudentAllergies");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "HealthVisits");

            migrationBuilder.RenameColumn(
                name: "Note",
                table: "VisitPrescriptions",
                newName: "UsageIns");

            migrationBuilder.RenameColumn(
                name: "ChangeQty",
                table: "MedicineStockLogs",
                newName: "Quantity");

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "StudentVaccinations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "StudentVaccinations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<float>(
                name: "CurrentWeight",
                table: "Students",
                type: "real",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<float>(
                name: "CurrentHeight",
                table: "Students",
                type: "real",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Guardian",
                table: "Students",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicalHistoryNotes",
                table: "Students",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Students",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "StudentAllergies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "MedicineStockLogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "MedicineStockLogs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "VisitId",
                table: "MedicineStockLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinStockLevel",
                table: "Medicines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "Medicines",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "MeasuredWeight",
                table: "HealthVisits",
                type: "real",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<float>(
                name: "MeasuredHeight",
                table: "HealthVisits",
                type: "real",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "DiseaseId",
                table: "HealthVisits",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "StudentUserId",
                table: "HealthVisits",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Students",
                table: "Students",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentVaccinations_UserId_VaccinationId",
                table: "StudentVaccinations",
                columns: new[] { "UserId", "VaccinationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentAllergies_UserId_AllergyId",
                table: "StudentAllergies",
                columns: new[] { "UserId", "AllergyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HealthVisits_StudentUserId",
                table: "HealthVisits",
                column: "StudentUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_HealthVisits_Students_StudentUserId",
                table: "HealthVisits",
                column: "StudentUserId",
                principalTable: "Students",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentAllergies_Students_UserId",
                table: "StudentAllergies",
                column: "UserId",
                principalTable: "Students",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Users_UserId",
                table: "Students",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentVaccinations_Students_UserId",
                table: "StudentVaccinations",
                column: "UserId",
                principalTable: "Students",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HealthVisits_Students_StudentUserId",
                table: "HealthVisits");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentAllergies_Students_UserId",
                table: "StudentAllergies");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Users_UserId",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentVaccinations_Students_UserId",
                table: "StudentVaccinations");

            migrationBuilder.DropIndex(
                name: "IX_StudentVaccinations_UserId_VaccinationId",
                table: "StudentVaccinations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Students",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_StudentAllergies_UserId_AllergyId",
                table: "StudentAllergies");

            migrationBuilder.DropIndex(
                name: "IX_HealthVisits_StudentUserId",
                table: "HealthVisits");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "StudentVaccinations");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Guardian",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "MedicalHistoryNotes",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "StudentAllergies");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "MedicineStockLogs");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "MedicineStockLogs");

            migrationBuilder.DropColumn(
                name: "VisitId",
                table: "MedicineStockLogs");

            migrationBuilder.DropColumn(
                name: "MinStockLevel",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "StudentUserId",
                table: "HealthVisits");

            migrationBuilder.RenameColumn(
                name: "UsageIns",
                table: "VisitPrescriptions",
                newName: "Note");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "MedicineStockLogs",
                newName: "ChangeQty");

            migrationBuilder.AlterColumn<bool>(
                name: "Status",
                table: "StudentVaccinations",
                type: "bit",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "StudentId",
                table: "StudentVaccinations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentWeight",
                table: "Students",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentHeight",
                table: "Students",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AddColumn<string>(
                name: "StudentId",
                table: "Students",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "Students",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentId",
                table: "StudentAllergies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "MeasuredWeight",
                table: "HealthVisits",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<decimal>(
                name: "MeasuredHeight",
                table: "HealthVisits",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<int>(
                name: "DiseaseId",
                table: "HealthVisits",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentId",
                table: "HealthVisits",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Students",
                table: "Students",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentVaccinations_StudentId_VaccinationId",
                table: "StudentVaccinations",
                columns: new[] { "StudentId", "VaccinationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentAllergies_StudentId_AllergyId",
                table: "StudentAllergies",
                columns: new[] { "StudentId", "AllergyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HealthVisits_StudentId",
                table: "HealthVisits",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_HealthVisits_Students_StudentId",
                table: "HealthVisits",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "StudentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentAllergies_Students_StudentId",
                table: "StudentAllergies",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "StudentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentVaccinations_Students_StudentId",
                table: "StudentVaccinations",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "StudentId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
