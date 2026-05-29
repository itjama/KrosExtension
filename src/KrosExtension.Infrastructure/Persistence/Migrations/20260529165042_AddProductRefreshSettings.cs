using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KrosExtension.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductRefreshSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountingYear",
                table: "TrackedCompanies",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductRefreshSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductType = table.Column<int>(type: "int", nullable: false),
                    RefreshTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductRefreshSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductRefreshSettings_ProductType",
                table: "ProductRefreshSettings",
                column: "ProductType",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductRefreshSettings");

            migrationBuilder.DropColumn(
                name: "AccountingYear",
                table: "TrackedCompanies");
        }
    }
}
