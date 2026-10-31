using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerConsumerInboxClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Consumer",
                table: "InboxMessages",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // Existing claims were message-level. They cannot match any new
            // per-consumer key, so they are preserved as 'legacy' rows and
            // removed later by inbox retention.
            migrationBuilder.Sql("UPDATE InboxMessages SET Consumer = 'legacy';");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InboxMessages",
                table: "InboxMessages");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "InboxMessages",
                newName: "MessageId");

            migrationBuilder.AlterColumn<string>(
                name: "Consumer",
                table: "InboxMessages",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_InboxMessages",
                table: "InboxMessages",
                columns: new[] { "MessageId", "Consumer" });

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_ConsumedOnUtc",
                table: "InboxMessages",
                column: "ConsumedOnUtc");
        }

        /// <inheritdoc />
        /// <remarks>
        /// Rolling back discards dedupe history: the old schema only supports one
        /// claim per message, so duplicate (MessageId) rows cannot be preserved.
        /// </remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxMessages");

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ConsumedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => x.Id);
                });
        }
    }
}
