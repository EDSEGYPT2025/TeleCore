using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeleCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddERPFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShiftSimCards_Shifts_ShiftId",
                table: "ShiftSimCards");

            migrationBuilder.DropForeignKey(
                name: "FK_ShiftSimCards_SimCards_SimCardId",
                table: "ShiftSimCards");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_SimCards_SimCardId",
                table: "Transactions");

            migrationBuilder.AlterColumn<decimal>(
                name: "PercentageFee",
                table: "CommissionRules",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftSimCards_Shifts_ShiftId",
                table: "ShiftSimCards",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftSimCards_SimCards_SimCardId",
                table: "ShiftSimCards",
                column: "SimCardId",
                principalTable: "SimCards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_SimCards_SimCardId",
                table: "Transactions",
                column: "SimCardId",
                principalTable: "SimCards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShiftSimCards_Shifts_ShiftId",
                table: "ShiftSimCards");

            migrationBuilder.DropForeignKey(
                name: "FK_ShiftSimCards_SimCards_SimCardId",
                table: "ShiftSimCards");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_SimCards_SimCardId",
                table: "Transactions");

            migrationBuilder.AlterColumn<decimal>(
                name: "PercentageFee",
                table: "CommissionRules",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftSimCards_Shifts_ShiftId",
                table: "ShiftSimCards",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftSimCards_SimCards_SimCardId",
                table: "ShiftSimCards",
                column: "SimCardId",
                principalTable: "SimCards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_SimCards_SimCardId",
                table: "Transactions",
                column: "SimCardId",
                principalTable: "SimCards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
