using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KrosExtension.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOlympDashboardSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OlympCompanyDashboardSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataReadServerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServerDisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DatabaseName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AccountingYear = table.Column<int>(type: "int", nullable: true),
                    SnapshotAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    HealthScore = table.Column<int>(type: "int", nullable: false),
                    ActivityScore = table.Column<int>(type: "int", nullable: false),
                    PayrollCurrentnessScore = table.Column<int>(type: "int", nullable: false),
                    PayrollConsistencyScore = table.Column<int>(type: "int", nullable: false),
                    PeriodRegularityScore = table.Column<int>(type: "int", nullable: false),
                    LastActivityDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastProcessedPeriod = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    CurrentMonthProcessed = table.Column<bool>(type: "bit", nullable: false),
                    ProcessedPeriodsInYear = table.Column<int>(type: "int", nullable: false),
                    Activity7Days = table.Column<long>(type: "bigint", nullable: false),
                    Activity30Days = table.Column<long>(type: "bigint", nullable: false),
                    NullStartTimeCount = table.Column<long>(type: "bigint", nullable: false),
                    MissingPeriodsCount = table.Column<int>(type: "int", nullable: false),
                    InconsistentPeriodsCount = table.Column<int>(type: "int", nullable: false),
                    TrendPercent = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    WarningFlags = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OlympCompanyDashboardSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OlympCompanyDashboardSnapshots_DataReadServerId_DatabaseName_SnapshotAt",
                table: "OlympCompanyDashboardSnapshots",
                columns: new[] { "DataReadServerId", "DatabaseName", "SnapshotAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OlympCompanyDashboardSnapshots_HealthScore",
                table: "OlympCompanyDashboardSnapshots",
                column: "HealthScore");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OlympCompanyDashboardSnapshots");
        }
    }
}
