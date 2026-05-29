using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KrosExtension.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOmegaDashboardSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OmegaCompanyDashboardSnapshots",
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
                    CompletionScore = table.Column<int>(type: "int", nullable: false),
                    BacklogAgingScore = table.Column<int>(type: "int", nullable: false),
                    MonthClosingScore = table.Column<int>(type: "int", nullable: false),
                    DataQualityScore = table.Column<int>(type: "int", nullable: false),
                    Activity7Days = table.Column<long>(type: "bigint", nullable: false),
                    Activity30Days = table.Column<long>(type: "bigint", nullable: false),
                    Previous7DaysActivity = table.Column<long>(type: "bigint", nullable: false),
                    TrendPercent = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    Registered30Days = table.Column<long>(type: "bigint", nullable: false),
                    Posted30Days = table.Column<long>(type: "bigint", nullable: false),
                    PostingRate30Days = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    BacklogCount = table.Column<long>(type: "bigint", nullable: false),
                    Backlog7DaysPlus = table.Column<long>(type: "bigint", nullable: false),
                    Backlog30DaysPlus = table.Column<long>(type: "bigint", nullable: false),
                    OldestOpenAgeDays = table.Column<int>(type: "int", nullable: true),
                    LastActivityDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PreviousMonthPostingRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    HistoricalPostingRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Imported30Days = table.Column<long>(type: "bigint", nullable: false),
                    Platform30Days = table.Column<long>(type: "bigint", nullable: false),
                    Manual30Days = table.Column<long>(type: "bigint", nullable: false),
                    DuplicateDocumentLinks = table.Column<long>(type: "bigint", nullable: false),
                    DataQualityIssues = table.Column<long>(type: "bigint", nullable: false),
                    WarningFlags = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OmegaCompanyDashboardSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OmegaCompanyDashboardSnapshots_DataReadServerId_DatabaseName_SnapshotAt",
                table: "OmegaCompanyDashboardSnapshots",
                columns: new[] { "DataReadServerId", "DatabaseName", "SnapshotAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OmegaCompanyDashboardSnapshots_HealthScore",
                table: "OmegaCompanyDashboardSnapshots",
                column: "HealthScore");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OmegaCompanyDashboardSnapshots");
        }
    }
}
