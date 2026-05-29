using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KrosExtension.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDataReadServers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataReadServers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ServerName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OlympUserName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OlympPassword = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    OmegaUserName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OmegaPassword = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    TrustServerCertificate = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataReadServers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataReadServers_ServerName",
                table: "DataReadServers",
                column: "ServerName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataReadServers");
        }
    }
}
