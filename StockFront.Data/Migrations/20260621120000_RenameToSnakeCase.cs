using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFront.Data.Migrations
{
    /// <summary>
    /// Renames every database object to snake_case to match the project naming convention
    /// (DB = snake_case, C# = PascalCase). The mapping itself is applied at runtime through
    /// <c>UseSnakeCaseNamingConvention()</c>; this migration brings an existing PascalCase schema
    /// in line with it. Tables, columns, indexes and foreign keys are all renamed.
    /// </summary>
    public partial class RenameToSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop foreign keys first (their constraint names are renamed via drop + re-add).
            migrationBuilder.DropForeignKey(name: "FK_Products_Categories_CategoryId", table: "Products");
            migrationBuilder.DropForeignKey(name: "FK_OrderLines_Orders_OrderId", table: "OrderLines");
            migrationBuilder.DropForeignKey(name: "FK_OrderLines_Products_ProductId", table: "OrderLines");
            migrationBuilder.DropForeignKey(name: "FK_StockItems_Products_ProductId", table: "StockItems");
            migrationBuilder.DropForeignKey(name: "FK_StockMovements_Products_ProductId", table: "StockMovements");

            // 2. Rename tables.
            migrationBuilder.RenameTable(name: "Categories", newName: "categories");
            migrationBuilder.RenameTable(name: "Orders", newName: "orders");
            migrationBuilder.RenameTable(name: "Products", newName: "products");
            migrationBuilder.RenameTable(name: "OrderLines", newName: "order_lines");
            migrationBuilder.RenameTable(name: "StockItems", newName: "stock_items");
            migrationBuilder.RenameTable(name: "Users", newName: "users");
            migrationBuilder.RenameTable(name: "StockMovements", newName: "stock_movements");

            // 3. Rename columns (table arguments use the new snake_case table names).
            migrationBuilder.RenameColumn(name: "Id", table: "categories", newName: "id");
            migrationBuilder.RenameColumn(name: "Name", table: "categories", newName: "name");

            migrationBuilder.RenameColumn(name: "Id", table: "orders", newName: "id");
            migrationBuilder.RenameColumn(name: "CustomerName", table: "orders", newName: "customer_name");
            migrationBuilder.RenameColumn(name: "Status", table: "orders", newName: "status");
            migrationBuilder.RenameColumn(name: "CreatedAt", table: "orders", newName: "created_at");

            migrationBuilder.RenameColumn(name: "Id", table: "products", newName: "id");
            migrationBuilder.RenameColumn(name: "Sku", table: "products", newName: "sku");
            migrationBuilder.RenameColumn(name: "Name", table: "products", newName: "name");
            migrationBuilder.RenameColumn(name: "Price", table: "products", newName: "price");
            migrationBuilder.RenameColumn(name: "CategoryId", table: "products", newName: "category_id");

            migrationBuilder.RenameColumn(name: "Id", table: "order_lines", newName: "id");
            migrationBuilder.RenameColumn(name: "OrderId", table: "order_lines", newName: "order_id");
            migrationBuilder.RenameColumn(name: "ProductId", table: "order_lines", newName: "product_id");
            migrationBuilder.RenameColumn(name: "Quantity", table: "order_lines", newName: "quantity");
            migrationBuilder.RenameColumn(name: "UnitPrice", table: "order_lines", newName: "unit_price");

            migrationBuilder.RenameColumn(name: "Id", table: "stock_items", newName: "id");
            migrationBuilder.RenameColumn(name: "ProductId", table: "stock_items", newName: "product_id");
            migrationBuilder.RenameColumn(name: "Quantity", table: "stock_items", newName: "quantity");
            migrationBuilder.RenameColumn(name: "Reserved", table: "stock_items", newName: "reserved");
            migrationBuilder.RenameColumn(name: "RowVersion", table: "stock_items", newName: "row_version");

            migrationBuilder.RenameColumn(name: "Id", table: "users", newName: "id");
            migrationBuilder.RenameColumn(name: "Email", table: "users", newName: "email");
            migrationBuilder.RenameColumn(name: "PasswordHash", table: "users", newName: "password_hash");
            migrationBuilder.RenameColumn(name: "DisplayName", table: "users", newName: "display_name");
            migrationBuilder.RenameColumn(name: "Role", table: "users", newName: "role");
            migrationBuilder.RenameColumn(name: "IsActive", table: "users", newName: "is_active");
            migrationBuilder.RenameColumn(name: "CreatedAt", table: "users", newName: "created_at");
            migrationBuilder.RenameColumn(name: "LastLoginAt", table: "users", newName: "last_login_at");

            migrationBuilder.RenameColumn(name: "Id", table: "stock_movements", newName: "id");
            migrationBuilder.RenameColumn(name: "ProductId", table: "stock_movements", newName: "product_id");
            migrationBuilder.RenameColumn(name: "Type", table: "stock_movements", newName: "type");
            migrationBuilder.RenameColumn(name: "Quantity", table: "stock_movements", newName: "quantity");
            migrationBuilder.RenameColumn(name: "Reason", table: "stock_movements", newName: "reason");
            migrationBuilder.RenameColumn(name: "PerformedBy", table: "stock_movements", newName: "performed_by");
            migrationBuilder.RenameColumn(name: "CreatedAt", table: "stock_movements", newName: "created_at");

            // 4. Rename indexes.
            migrationBuilder.RenameIndex(name: "IX_Orders_Status", table: "orders", newName: "ix_orders_status");
            migrationBuilder.RenameIndex(name: "IX_Orders_Status_CreatedAt", table: "orders", newName: "ix_orders_status_created_at");
            migrationBuilder.RenameIndex(name: "IX_Products_CategoryId", table: "products", newName: "ix_products_category_id");
            migrationBuilder.RenameIndex(name: "IX_Products_Sku", table: "products", newName: "ix_products_sku");
            migrationBuilder.RenameIndex(name: "IX_OrderLines_OrderId", table: "order_lines", newName: "ix_order_lines_order_id");
            migrationBuilder.RenameIndex(name: "IX_OrderLines_ProductId", table: "order_lines", newName: "ix_order_lines_product_id");
            migrationBuilder.RenameIndex(name: "IX_StockItems_ProductId", table: "stock_items", newName: "ix_stock_items_product_id");
            migrationBuilder.RenameIndex(name: "IX_Users_Email", table: "users", newName: "ix_users_email");
            migrationBuilder.RenameIndex(name: "IX_StockMovements_CreatedAt", table: "stock_movements", newName: "ix_stock_movements_created_at");
            migrationBuilder.RenameIndex(name: "IX_StockMovements_ProductId", table: "stock_movements", newName: "ix_stock_movements_product_id");

            // 5. Re-create foreign keys with snake_case constraint names.
            migrationBuilder.AddForeignKey(
                name: "fk_products_categories_category_id",
                table: "products",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_order_lines_orders_order_id",
                table: "order_lines",
                column: "order_id",
                principalTable: "orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_order_lines_products_product_id",
                table: "order_lines",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_items_products_product_id",
                table: "stock_items",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movements_products_product_id",
                table: "stock_movements",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse of Up: drop snake_case FKs, rename indexes/columns/tables back, re-add PascalCase FKs.
            migrationBuilder.DropForeignKey(name: "fk_products_categories_category_id", table: "products");
            migrationBuilder.DropForeignKey(name: "fk_order_lines_orders_order_id", table: "order_lines");
            migrationBuilder.DropForeignKey(name: "fk_order_lines_products_product_id", table: "order_lines");
            migrationBuilder.DropForeignKey(name: "fk_stock_items_products_product_id", table: "stock_items");
            migrationBuilder.DropForeignKey(name: "fk_stock_movements_products_product_id", table: "stock_movements");

            migrationBuilder.RenameIndex(name: "ix_orders_status", table: "orders", newName: "IX_Orders_Status");
            migrationBuilder.RenameIndex(name: "ix_orders_status_created_at", table: "orders", newName: "IX_Orders_Status_CreatedAt");
            migrationBuilder.RenameIndex(name: "ix_products_category_id", table: "products", newName: "IX_Products_CategoryId");
            migrationBuilder.RenameIndex(name: "ix_products_sku", table: "products", newName: "IX_Products_Sku");
            migrationBuilder.RenameIndex(name: "ix_order_lines_order_id", table: "order_lines", newName: "IX_OrderLines_OrderId");
            migrationBuilder.RenameIndex(name: "ix_order_lines_product_id", table: "order_lines", newName: "IX_OrderLines_ProductId");
            migrationBuilder.RenameIndex(name: "ix_stock_items_product_id", table: "stock_items", newName: "IX_StockItems_ProductId");
            migrationBuilder.RenameIndex(name: "ix_users_email", table: "users", newName: "IX_Users_Email");
            migrationBuilder.RenameIndex(name: "ix_stock_movements_created_at", table: "stock_movements", newName: "IX_StockMovements_CreatedAt");
            migrationBuilder.RenameIndex(name: "ix_stock_movements_product_id", table: "stock_movements", newName: "IX_StockMovements_ProductId");

            migrationBuilder.RenameColumn(name: "id", table: "categories", newName: "Id");
            migrationBuilder.RenameColumn(name: "name", table: "categories", newName: "Name");

            migrationBuilder.RenameColumn(name: "id", table: "orders", newName: "Id");
            migrationBuilder.RenameColumn(name: "customer_name", table: "orders", newName: "CustomerName");
            migrationBuilder.RenameColumn(name: "status", table: "orders", newName: "Status");
            migrationBuilder.RenameColumn(name: "created_at", table: "orders", newName: "CreatedAt");

            migrationBuilder.RenameColumn(name: "id", table: "products", newName: "Id");
            migrationBuilder.RenameColumn(name: "sku", table: "products", newName: "Sku");
            migrationBuilder.RenameColumn(name: "name", table: "products", newName: "Name");
            migrationBuilder.RenameColumn(name: "price", table: "products", newName: "Price");
            migrationBuilder.RenameColumn(name: "category_id", table: "products", newName: "CategoryId");

            migrationBuilder.RenameColumn(name: "id", table: "order_lines", newName: "Id");
            migrationBuilder.RenameColumn(name: "order_id", table: "order_lines", newName: "OrderId");
            migrationBuilder.RenameColumn(name: "product_id", table: "order_lines", newName: "ProductId");
            migrationBuilder.RenameColumn(name: "quantity", table: "order_lines", newName: "Quantity");
            migrationBuilder.RenameColumn(name: "unit_price", table: "order_lines", newName: "UnitPrice");

            migrationBuilder.RenameColumn(name: "id", table: "stock_items", newName: "Id");
            migrationBuilder.RenameColumn(name: "product_id", table: "stock_items", newName: "ProductId");
            migrationBuilder.RenameColumn(name: "quantity", table: "stock_items", newName: "Quantity");
            migrationBuilder.RenameColumn(name: "reserved", table: "stock_items", newName: "Reserved");
            migrationBuilder.RenameColumn(name: "row_version", table: "stock_items", newName: "RowVersion");

            migrationBuilder.RenameColumn(name: "id", table: "users", newName: "Id");
            migrationBuilder.RenameColumn(name: "email", table: "users", newName: "Email");
            migrationBuilder.RenameColumn(name: "password_hash", table: "users", newName: "PasswordHash");
            migrationBuilder.RenameColumn(name: "display_name", table: "users", newName: "DisplayName");
            migrationBuilder.RenameColumn(name: "role", table: "users", newName: "Role");
            migrationBuilder.RenameColumn(name: "is_active", table: "users", newName: "IsActive");
            migrationBuilder.RenameColumn(name: "created_at", table: "users", newName: "CreatedAt");
            migrationBuilder.RenameColumn(name: "last_login_at", table: "users", newName: "LastLoginAt");

            migrationBuilder.RenameColumn(name: "id", table: "stock_movements", newName: "Id");
            migrationBuilder.RenameColumn(name: "product_id", table: "stock_movements", newName: "ProductId");
            migrationBuilder.RenameColumn(name: "type", table: "stock_movements", newName: "Type");
            migrationBuilder.RenameColumn(name: "quantity", table: "stock_movements", newName: "Quantity");
            migrationBuilder.RenameColumn(name: "reason", table: "stock_movements", newName: "Reason");
            migrationBuilder.RenameColumn(name: "performed_by", table: "stock_movements", newName: "PerformedBy");
            migrationBuilder.RenameColumn(name: "created_at", table: "stock_movements", newName: "CreatedAt");

            migrationBuilder.RenameTable(name: "categories", newName: "Categories");
            migrationBuilder.RenameTable(name: "orders", newName: "Orders");
            migrationBuilder.RenameTable(name: "products", newName: "Products");
            migrationBuilder.RenameTable(name: "order_lines", newName: "OrderLines");
            migrationBuilder.RenameTable(name: "stock_items", newName: "StockItems");
            migrationBuilder.RenameTable(name: "users", newName: "Users");
            migrationBuilder.RenameTable(name: "stock_movements", newName: "StockMovements");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderLines_Orders_OrderId",
                table: "OrderLines",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderLines_Products_ProductId",
                table: "OrderLines",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockItems_Products_ProductId",
                table: "StockItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Products_ProductId",
                table: "StockMovements",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}