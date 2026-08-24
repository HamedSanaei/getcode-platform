using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GetCode.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTraceContextToOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "span_id",
                table: "outbox_messages",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "trace_id",
                table: "outbox_messages",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "span_id",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "trace_id",
                table: "outbox_messages");
        }
    }
}
