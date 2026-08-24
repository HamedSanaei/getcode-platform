using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GetCode.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "countries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    default_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_countries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "services",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    default_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_services", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "country_localized_names",
                columns: table => new
                {
                    culture_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_country_localized_names", x => new { x.country_id, x.culture_code });
                    table.ForeignKey(
                        name: "fk_country_localized_names_countries",
                        column: x => x.country_id,
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_localized_names",
                columns: table => new
                {
                    culture_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_localized_names", x => new { x.service_id, x.culture_code });
                    table.ForeignKey(
                        name: "fk_service_localized_names_services",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_countries__code",
                table: "countries",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_services__slug",
                table: "services",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "country_localized_names");

            migrationBuilder.DropTable(
                name: "service_localized_names");

            migrationBuilder.DropTable(
                name: "countries");

            migrationBuilder.DropTable(
                name: "services");
        }
    }
}
