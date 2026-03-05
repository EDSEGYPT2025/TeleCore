using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeleCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMobileNodesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MobileNodeId",
                table: "SimCards",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MobileNodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceUniqueId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceModel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublicKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAuthorized = table.Column<bool>(type: "bit", nullable: false),
                    LastSeen = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MobileNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MobileNodes_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SimCards_MobileNodeId",
                table: "SimCards",
                column: "MobileNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_MobileNodes_BranchId",
                table: "MobileNodes",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_SimCards_MobileNodes_MobileNodeId",
                table: "SimCards",
                column: "MobileNodeId",
                principalTable: "MobileNodes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SimCards_MobileNodes_MobileNodeId",
                table: "SimCards");

            migrationBuilder.DropTable(
                name: "MobileNodes");

            migrationBuilder.DropIndex(
                name: "IX_SimCards_MobileNodeId",
                table: "SimCards");

            migrationBuilder.DropColumn(
                name: "MobileNodeId",
                table: "SimCards");
        }
    }
}
