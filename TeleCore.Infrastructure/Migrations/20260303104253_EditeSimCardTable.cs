using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeleCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EditeSimCardTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedDeviceId",
                table: "SimCards",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedDeviceId",
                table: "SimCards");
        }
    }
}
