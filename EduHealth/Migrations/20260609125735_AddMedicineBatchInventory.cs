using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduHealth.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicineBatchInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MedicineBatchId",
                table: "MedicineStockLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MedicineBatches",
                columns: table => new
                {
                    MedicineBatchId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MedicineId = table.Column<int>(type: "int", nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InitialQuantity = table.Column<int>(type: "int", nullable: false),
                    RemainingQuantity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineBatches", x => x.MedicineBatchId);
                    table.ForeignKey(
                        name: "FK_MedicineBatches_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicines",
                        principalColumn: "MedicineId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicineBatches_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicineStockLogs_MedicineBatchId",
                table: "MedicineStockLogs",
                column: "MedicineBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineBatches_Code",
                table: "MedicineBatches",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicineBatches_CreatedByUserId",
                table: "MedicineBatches",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineBatches_MedicineId_ExpiryDate_Status",
                table: "MedicineBatches",
                columns: new[] { "MedicineId", "ExpiryDate", "Status" });

            migrationBuilder.Sql(
                """
                INSERT INTO [MedicineBatches]
                    ([Code], [MedicineId], [BatchNumber], [ReceivedAt], [ExpiryDate],
                     [InitialQuantity], [RemainingQuantity], [Status], [Note],
                     [CreatedByUserId], [CreatedAt], [UpdatedAt])
                SELECT
                    CONCAT('LEGACY-', m.[Code]),
                    m.[MedicineId],
                    latestImport.[BatchNumber],
                    COALESCE(latestImport.[CreatedAt], m.[CreatedAt]),
                    COALESCE(
                        m.[NearestExpiryDate],
                        latestImport.[ExpiryDate],
                        DATEADD(day, 365, CAST(GETDATE() AS date))
                    ),
                    m.[StockQuantity],
                    m.[StockQuantity],
                    'ACTIVE',
                    'Migrated from legacy medicine-level inventory',
                    NULL,
                    GETDATE(),
                    GETDATE()
                FROM [Medicines] m
                OUTER APPLY (
                    SELECT TOP (1)
                        l.[BatchNumber],
                        l.[CreatedAt],
                        l.[ExpiryDate]
                    FROM [MedicineStockLogs] l
                    WHERE l.[MedicineId] = m.[MedicineId]
                      AND l.[Type] IN ('IMPORT', 'STOCK_IN')
                    ORDER BY
                        CASE WHEN l.[ExpiryDate] IS NULL THEN 1 ELSE 0 END,
                        l.[ExpiryDate],
                        l.[CreatedAt] DESC
                ) latestImport
                WHERE m.[StockQuantity] > 0;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicineStockLogs_MedicineBatches_MedicineBatchId",
                table: "MedicineStockLogs",
                column: "MedicineBatchId",
                principalTable: "MedicineBatches",
                principalColumn: "MedicineBatchId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicineStockLogs_MedicineBatches_MedicineBatchId",
                table: "MedicineStockLogs");

            migrationBuilder.DropTable(
                name: "MedicineBatches");

            migrationBuilder.DropIndex(
                name: "IX_MedicineStockLogs_MedicineBatchId",
                table: "MedicineStockLogs");

            migrationBuilder.DropColumn(
                name: "MedicineBatchId",
                table: "MedicineStockLogs");
        }
    }
}
