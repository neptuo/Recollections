using Microsoft.EntityFrameworkCore.Migrations;
using Neptuo.Recollections.Migrations;

#nullable disable

namespace Neptuo.Recollections.Accounts.Migrations
{
    /// <inheritdoc />
    public partial class InheritSharing : MigrationWithSchema<DataContext>
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Permission",
                table: "UserConnections",
                schema: Schema.Name,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OtherPermission",
                table: "UserConnections",
                schema: Schema.Name,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Permission",
                table: "UserConnections",
                schema: Schema.Name);

            migrationBuilder.DropColumn(
                name: "OtherPermission",
                table: "UserConnections",
                schema: Schema.Name);
        }
    }
}
