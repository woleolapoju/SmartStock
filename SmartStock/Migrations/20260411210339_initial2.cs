using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartStock.Migrations
{
    /// <inheritdoc />
    public partial class initial2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockAdjustmentLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    LocationType = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    QuantityBefore = table.Column<int>(type: "int", nullable: false),
                    QuantityChange = table.Column<int>(type: "int", nullable: false),
                    QuantityAfter = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AdjustedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AdjustedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustmentLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockAdjustmentLogs_AspNetUsers_AdjustedByUserId",
                        column: x => x.AdjustedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StockAdjustmentLogs_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLogs_AdjustedByUserId",
                table: "StockAdjustmentLogs",
                column: "AdjustedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLogs_LocationType_LocationId",
                table: "StockAdjustmentLogs",
                columns: new[] { "LocationType", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLogs_ProductId_AdjustedAt",
                table: "StockAdjustmentLogs",
                columns: new[] { "ProductId", "AdjustedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockAdjustmentLogs");
        }
    }
}
