using Microsoft.EntityFrameworkCore.Migrations;
using Neptuo.Recollections.Migrations;


#nullable disable

namespace Neptuo.Recollections.Entries.Migrations
{
    /// <inheritdoc />
    public partial class InheritSharing : MigrationWithSchema<DataContext>
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSharingInherited",
                table: "Stories",
                schema: Schema.Name,
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSharingInherited",
                table: "Entries",
                schema: Schema.Name,
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSharingInherited",
                table: "Beings",
                schema: Schema.Name,
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSharingInherited",
                table: "Stories",
                schema: Schema.Name);

            migrationBuilder.DropColumn(
                name: "IsSharingInherited",
                table: "Entries",
                schema: Schema.Name);

            migrationBuilder.DropColumn(
                name: "IsSharingInherited",
                table: "Beings",
                schema: Schema.Name);
        }
    }
}
