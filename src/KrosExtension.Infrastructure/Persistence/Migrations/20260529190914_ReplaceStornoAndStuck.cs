using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KrosExtension.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceStornoAndStuck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DuplicateDocumentLinks",
                table: "OmegaCompanyDashboardSnapshots",
                newName: "PreAccountedStuck");

            migrationBuilder.AddColumn<decimal>(
                name: "StornoRate30Days",
                table: "OmegaCompanyDashboardSnapshots",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StornoRate30Days",
                table: "OmegaCompanyDashboardSnapshots");

            migrationBuilder.RenameColumn(
                name: "PreAccountedStuck",
                table: "OmegaCompanyDashboardSnapshots",
                newName: "DuplicateDocumentLinks");
        }
    }
}
