using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StoreLedgerEntryTypeAsString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "StockLedgerEntries",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            // The ALTER casts existing ints to digit strings ('1', '2', ...); map
            // them to their enum names so stored values match what EF now writes.
            migrationBuilder.Sql(
                "UPDATE StockLedgerEntries " +
                "SET [Type] = CASE [Type] " +
                "WHEN '1' THEN 'Reserve' " +
                "WHEN '2' THEN 'Release' " +
                "WHEN '3' THEN 'Initial' " +
                "WHEN '4' THEN 'Adjustment' " +
                "ELSE [Type] END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Un-map names to numeric strings first, so the int ALTER can convert them.
            migrationBuilder.Sql(
                "UPDATE StockLedgerEntries " +
                "SET [Type] = CASE [Type] " +
                "WHEN 'Reserve' THEN '1' " +
                "WHEN 'Release' THEN '2' " +
                "WHEN 'Initial' THEN '3' " +
                "WHEN 'Adjustment' THEN '4' " +
                "ELSE [Type] END");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "StockLedgerEntries",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);
        }
    }
}
