using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserAddressToCleanDDD : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Addresses",
                schema: "app",
                table: "Addresses");

            migrationBuilder.RenameTable(
                name: "Addresses",
                schema: "app",
                newName: "UserAddresses",
                newSchema: "app");

            migrationBuilder.RenameIndex(
                name: "IX_Addresses_UserId",
                schema: "app",
                table: "UserAddresses",
                newName: "IX_UserAddresses_UserId");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                schema: "app",
                table: "UserAddresses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedOnUtc",
                schema: "app",
                table: "UserAddresses",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "app",
                table: "UserAddresses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                schema: "app",
                table: "UserAddresses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedOnUtc",
                schema: "app",
                table: "UserAddresses",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserAddresses",
                schema: "app",
                table: "UserAddresses",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UserAddresses",
                schema: "app",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "app",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "CreatedOnUtc",
                schema: "app",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "app",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "app",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "UpdatedOnUtc",
                schema: "app",
                table: "UserAddresses");

            migrationBuilder.RenameTable(
                name: "UserAddresses",
                schema: "app",
                newName: "Addresses",
                newSchema: "app");

            migrationBuilder.RenameIndex(
                name: "IX_UserAddresses_UserId",
                schema: "app",
                table: "Addresses",
                newName: "IX_Addresses_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Addresses",
                schema: "app",
                table: "Addresses",
                column: "Id");
        }
    }
}
