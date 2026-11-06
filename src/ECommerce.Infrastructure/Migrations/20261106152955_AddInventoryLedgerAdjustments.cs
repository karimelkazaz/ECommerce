using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryLedgerAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockLedgerEntries_OrderId",
                table: "StockLedgerEntries");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrderId",
                table: "StockLedgerEntries",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateIndex(
                name: "IX_StockLedgerEntries_OrderId_Type",
                table: "StockLedgerEntries",
                columns: new[] { "OrderId", "Type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockLedgerEntries_OrderId_Type",
                table: "StockLedgerEntries");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrderId",
                table: "StockLedgerEntries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockLedgerEntries_OrderId",
                table: "StockLedgerEntries",
                column: "OrderId");
        }
    }
}
