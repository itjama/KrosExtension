using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KrosExtension.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTimelinessScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TimelinessScore",
                table: "OmegaCompanyDashboardSnapshots",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimelinessScore",
                table: "OmegaCompanyDashboardSnapshots");
        }
    }
}
