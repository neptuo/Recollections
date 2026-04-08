using Microsoft.EntityFrameworkCore.Migrations;
using Neptuo.Recollections.Migrations;

#nullable disable

namespace Neptuo.Recollections.Entries.Migrations
{
    /// <inheritdoc />
    public partial class VideoOriginalSize : MigrationWithSchema<DataContext>
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OriginalSize",
                table: "Videos",
                schema: Schema.Name,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalSize",
                table: "Videos",
                schema: Schema.Name);
        }
    }
}
