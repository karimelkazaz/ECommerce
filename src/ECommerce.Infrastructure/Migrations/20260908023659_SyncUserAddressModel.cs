using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncUserAddressModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDefaultBilling",
                schema: "app",
                table: "UserAddresses");

            migrationBuilder.RenameColumn(
                name: "IsDefaultShipping",
                schema: "app",
                table: "UserAddresses",
                newName: "IsDefault");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsDefault",
                schema: "app",
                table: "UserAddresses",
                newName: "IsDefaultShipping");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefaultBilling",
                schema: "app",
                table: "UserAddresses",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
