using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KrosExtension.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPostingLagMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PostingLagBvAvgDays",
                table: "OmegaCompanyDashboardSnapshots",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PostingLagBvCount",
                table: "OmegaCompanyDashboardSnapshots",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "PostingLagBvMaxDays",
                table: "OmegaCompanyDashboardSnapshots",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PostingLagDfAvgDays",
                table: "OmegaCompanyDashboardSnapshots",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PostingLagDfCount",
                table: "OmegaCompanyDashboardSnapshots",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "PostingLagDfMaxDays",
                table: "OmegaCompanyDashboardSnapshots",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PostingLagOfAvgDays",
                table: "OmegaCompanyDashboardSnapshots",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PostingLagOfCount",
                table: "OmegaCompanyDashboardSnapshots",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "PostingLagOfMaxDays",
                table: "OmegaCompanyDashboardSnapshots",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PostingLagPdAvgDays",
                table: "OmegaCompanyDashboardSnapshots",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PostingLagPdCount",
                table: "OmegaCompanyDashboardSnapshots",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "PostingLagPdMaxDays",
                table: "OmegaCompanyDashboardSnapshots",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PostingLagBvAvgDays",
                table: "OmegaCompanyDashboardSnapshots");

            migrationBuilder.DropColumn(
                name: "PostingLagBvCount",
                table: "OmegaCompanyDashboardSnapshots");

            migrationBuilder.DropColumn(
                name: "PostingLagBvMaxDays",
                table: "OmegaCompanyDashboardSnapshots");

            migrationBuilder.DropColumn(
                name: "PostingLagDfAvgDays",
                table: "OmegaCompanyDashboardSnapshots");

            migrationBuilder.DropColumn(
                name: "PostingLagDfCount",
                table: "OmegaCompanyDashboardSnapshots");

            migrationBuilder.DropColumn(
                name: "PostingLagDfMaxDays",
                table: "OmegaCompanyDashboardSnapshots");

            migrationBuilder.DropColumn(
                name: "PostingLagOfAvgDays",
                table: "OmegaCompanyDashboardSnapshots");

            migrationBuilder.DropColumn(
                name: "PostingLagOfCount",
                table: "OmegaCompanyDashboardSnapshots");

            migrationBuilder.DropColumn(
                name: "PostingLagOfMaxDays",
                table: "OmegaCompanyDashboardSnapshots");

            migrationBuilder.DropColumn(
                name: "PostingLagPdAvgDays",
                table: "OmegaCompanyDashboardSnapshots");

            migrationBuilder.DropColumn(
                name: "PostingLagPdCount",
                table: "OmegaCompanyDashboardSnapshots");

            migrationBuilder.DropColumn(
                name: "PostingLagPdMaxDays",
                table: "OmegaCompanyDashboardSnapshots");
        }
    }
}
