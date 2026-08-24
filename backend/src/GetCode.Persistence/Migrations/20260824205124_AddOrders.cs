using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GetCode.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quote_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    country_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    service_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    product_type_key = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    pricing_rule_version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    payment_state = table.Column<int>(type: "integer", nullable: false),
                    fulfillment_state = table.Column<int>(type: "integer", nullable: false),
                    provider_operation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.order_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_orders__customer_id_idempotency_key",
                table: "orders",
                columns: new[] { "customer_id", "idempotency_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "orders");
        }
    }
}
