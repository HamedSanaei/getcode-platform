using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GetCode.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSkus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_skus",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_type = table.Column<int>(type: "integer", nullable: false),
                    is_offered = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_skus", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_skus_countries",
                        column: x => x.country_id,
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_skus_services",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_product_skus__country_id_service_id_product_type",
                table: "product_skus",
                columns: new[] { "country_id", "service_id", "product_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_skus__service_id",
                table: "product_skus",
                column: "service_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_skus");
        }
    }
}
