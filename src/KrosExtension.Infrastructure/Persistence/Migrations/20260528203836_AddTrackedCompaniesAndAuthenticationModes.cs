using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KrosExtension.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackedCompaniesAndAuthenticationModes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OlympAuthenticationMode",
                table: "DataReadServers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OmegaAuthenticationMode",
                table: "DataReadServers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TrackedCompanies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductType = table.Column<int>(type: "int", nullable: false),
                    DataReadServerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DatabaseName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsTracked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedCompanies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackedCompanies_ProductType_DataReadServerId_DatabaseName",
                table: "TrackedCompanies",
                columns: new[] { "ProductType", "DataReadServerId", "DatabaseName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrackedCompanies");

            migrationBuilder.DropColumn(
                name: "OlympAuthenticationMode",
                table: "DataReadServers");

            migrationBuilder.DropColumn(
                name: "OmegaAuthenticationMode",
                table: "DataReadServers");
        }
    }
}
