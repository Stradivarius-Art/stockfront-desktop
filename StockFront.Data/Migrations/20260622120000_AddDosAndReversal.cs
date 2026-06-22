using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFront.Data.Migrations
{
    /// <summary>
    /// Adds the inputs for Days-of-Supply status (product created_at, lead_time_days,
    /// safety_buffer_days, is_seasonal) and the reversal/audit fields on stock_movements
    /// (reverses_movement_id self-reference, comment). Also makes stock_movements.quantity signed:
    /// existing write-offs are flipped to negative so the journal direction lives in the sign.
    /// Hand-written — the project does not run <c>dotnet ef</c> locally.
    /// </summary>
    public partial class AddDosAndReversal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "products",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.AddColumn<int>(
                name: "lead_time_days",
                table: "products",
                type: "int",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.AddColumn<int>(
                name: "safety_buffer_days",
                table: "products",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<bool>(
                name: "is_seasonal",
                table: "products",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "reverses_movement_id",
                table: "stock_movements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "comment",
                table: "stock_movements",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Quantity is now signed: write-offs become negative. (Only Receipt/WriteOff exist yet.)
            migrationBuilder.Sql("UPDATE stock_movements SET quantity = -quantity WHERE type = 'WriteOff';");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_reverses_movement_id",
                table: "stock_movements",
                column: "reverses_movement_id");

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movements_stock_movements_reverses_movement_id",
                table: "stock_movements",
                column: "reverses_movement_id",
                principalTable: "stock_movements",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_stock_movements_stock_movements_reverses_movement_id",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ix_stock_movements_reverses_movement_id",
                table: "stock_movements");

            // Restore the unsigned-quantity convention before dropping the new columns.
            migrationBuilder.Sql("UPDATE stock_movements SET quantity = -quantity WHERE type = 'WriteOff';");

            migrationBuilder.DropColumn(name: "comment", table: "stock_movements");
            migrationBuilder.DropColumn(name: "reverses_movement_id", table: "stock_movements");

            migrationBuilder.DropColumn(name: "is_seasonal", table: "products");
            migrationBuilder.DropColumn(name: "safety_buffer_days", table: "products");
            migrationBuilder.DropColumn(name: "lead_time_days", table: "products");
            migrationBuilder.DropColumn(name: "created_at", table: "products");
        }
    }
}