using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeleCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateERPStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "NetworkFee",
                table: "Transactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ProviderResponseMessage",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyDepositLimit",
                table: "SimCards",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyWithdrawLimit",
                table: "SimCards",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyDepositLimit",
                table: "SimCards",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyWithdrawLimit",
                table: "SimCards",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "NationalId",
                table: "SimCards",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerName",
                table: "SimCards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "SimCards",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionType",
                table: "CommissionRules",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Branches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Branches",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSimCards_SimCardId",
                table: "ShiftSimCards",
                column: "SimCardId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftSimCards_SimCards_SimCardId",
                table: "ShiftSimCards",
                column: "SimCardId",
                principalTable: "SimCards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShiftSimCards_SimCards_SimCardId",
                table: "ShiftSimCards");

            migrationBuilder.DropIndex(
                name: "IX_ShiftSimCards_SimCardId",
                table: "ShiftSimCards");

            migrationBuilder.DropColumn(
                name: "NetworkFee",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ProviderResponseMessage",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "DailyDepositLimit",
                table: "SimCards");

            migrationBuilder.DropColumn(
                name: "DailyWithdrawLimit",
                table: "SimCards");

            migrationBuilder.DropColumn(
                name: "MonthlyDepositLimit",
                table: "SimCards");

            migrationBuilder.DropColumn(
                name: "MonthlyWithdrawLimit",
                table: "SimCards");

            migrationBuilder.DropColumn(
                name: "NationalId",
                table: "SimCards");

            migrationBuilder.DropColumn(
                name: "OwnerName",
                table: "SimCards");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "SimCards");

            migrationBuilder.DropColumn(
                name: "TransactionType",
                table: "CommissionRules");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Branches");
        }
    }
}
