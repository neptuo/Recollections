using Microsoft.EntityFrameworkCore.Migrations;
using Neptuo.Recollections.Migrations;


#nullable disable

namespace Neptuo.Recollections.Entries.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStoryOrder : MigrationWithSchema<DataContext>
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                table: "Stories",
                schema: Schema.Name);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Stories",
                schema: Schema.Name,
                nullable: false,
                defaultValue: 0);
        }
    }
}
