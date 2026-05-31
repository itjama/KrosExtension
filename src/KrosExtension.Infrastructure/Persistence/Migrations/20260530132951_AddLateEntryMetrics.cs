using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KrosExtension.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLateEntryMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LateEntryDfAvgDays",
                table: "OmegaCompanyDashboardSnapshots",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LateEntryDfCount",
                table: "OmegaCompanyDashboardSnapshots",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "LateEntryDfMaxDays",
                table: "OmegaCompanyDashboardSnapshots",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LateEntryOfAvgDays",
                table: "OmegaCompanyDashboardSnapshots",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LateEntryOfCount",
                table: "OmegaCompanyDashboardSnapshots",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "LateEntryOfMaxDays",
                table: "OmegaCompanyDashboardSnapshots",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LateEntryPdAvgDays",
                table: "OmegaCompanyDashboardSnapshots",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LateEntryPdCount",
                table: "OmegaCompanyDashboardSnapshots",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "LateEntryPdMaxDays",
                table: "OmegaCompanyDashboardSnapshots",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LateEntryDfAvgDays",
                table: "OmegaCompanyDashboardSnapshots");

            migrationBuilder.DropColumn(
                name: "LateEntryDfCount",
                table: "OmegaCompanyDashboardSnapshots");

            migrationBuilder.DropColumn(
                name: "LateEntryDfMaxDays",
                table: "OmegaCompanyDashboardSnapshots");

            migrationBuilder.DropColumn(
                name: "LateEntryOfAvgDays",
                table: "OmegaCompanyDashboardSnapshots");

            migrationBuilder.DropColumn(
                name: "LateEntryOfCount",
                table: "OmegaCompanyDashboardSnapshots");

            migrationBuilder.DropColumn(
                name: "LateEntryOfMaxDays",
                table: "OmegaCompanyDashboardSnapshots");

            migrationBuilder.DropColumn(
                name: "LateEntryPdAvgDays",
                table: "OmegaCompanyDashboardSnapshots");

            migrationBuilder.DropColumn(
                name: "LateEntryPdCount",
                table: "OmegaCompanyDashboardSnapshots");

            migrationBuilder.DropColumn(
                name: "LateEntryPdMaxDays",
                table: "OmegaCompanyDashboardSnapshots");
        }
    }
}
