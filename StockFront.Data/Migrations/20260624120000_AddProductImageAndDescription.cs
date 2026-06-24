using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFront.Data.Migrations
{
    /// <summary>
    /// Adds the storefront presentation fields on products: <c>description</c> (long card text) and
    /// <c>image_path</c> (relative file name into the local image store). Both are optional and editable
    /// on the storefront by an admin; stock is untouched. Hand-written — the project does not run
    /// <c>dotnet ef</c> locally.
    /// </summary>
    public partial class AddProductImageAndDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "products",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "image_path",
                table: "products",
                type: "varchar(512)",
                maxLength: 512,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "image_path", table: "products");
            migrationBuilder.DropColumn(name: "description", table: "products");
        }
    }
}