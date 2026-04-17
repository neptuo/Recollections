using Microsoft.EntityFrameworkCore.Migrations;
using Neptuo.Recollections.Migrations;

#nullable disable

namespace Neptuo.Recollections.Entries.Migrations
{
    /// <inheritdoc />
    public partial class EntryTrackTotalDistance : MigrationWithSchema<DataContext>
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "TrackTotalDistance",
                table: "Entries",
                schema: Schema.Name,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrackTotalDistance",
                schema: Schema.Name,
                table: "Entries");
        }
    }
}
