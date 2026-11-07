using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stockroom.Api.Migrations
{
    /// <inheritdoc />
    public partial class StockLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE FUNCTION reject_movement_mutation() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Stock history is immutable' USING ERRCODE = '23514'; END;
                $$;
                """);
            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uuid", nullable: false),
                    Fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Delta = table.Column<int>(type: "integer", nullable: false),
                    QuantityBefore = table.Column<int>(type: "integer", nullable: false),
                    QuantityAfter = table.Column<int>(type: "integer", nullable: false),
                    CostPrice = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.CheckConstraint("CK_Movement_Balance", "\"QuantityBefore\" >= 0 AND \"QuantityAfter\" BETWEEN 0 AND 1000000000 AND \"QuantityAfter\" = \"QuantityBefore\" + \"Delta\"");
                    table.CheckConstraint("CK_Movement_Kind", "(\"Kind\" = 'in' AND \"Delta\" > 0) OR (\"Kind\" = 'out' AND \"Delta\" < 0) OR (\"Kind\" = 'adjustment' AND \"Delta\" <> 0)");
                    table.CheckConstraint("CK_Movement_Reason", "length(btrim(\"Reason\")) BETWEEN 3 AND 500");
                    table.ForeignKey(
                        name: "FK_StockMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_CreatedAt",
                table: "StockMovements",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_IdempotencyKey",
                table: "StockMovements",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId_CreatedAt",
                table: "StockMovements",
                columns: new[] { "ProductId", "CreatedAt" });
            migrationBuilder.Sql("""
                CREATE TRIGGER immutable_stock_history BEFORE UPDATE OR DELETE ON "StockMovements"
                FOR EACH ROW EXECUTE FUNCTION reject_movement_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockMovements");
            migrationBuilder.Sql("DROP FUNCTION reject_movement_mutation()");
        }
    }
}

