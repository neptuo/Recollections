using Microsoft.EntityFrameworkCore.Migrations;
using Neptuo.Recollections.Migrations;

#nullable disable

namespace Neptuo.Recollections.Entries.Migrations
{
    /// <inheritdoc />
    public partial class EntryTrackBlob : MigrationWithSchema<DataContext>
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "TrackAltitude",
                table: "Entries",
                schema: Schema.Name,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackData",
                table: "Entries",
                schema: Schema.Name,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TrackLatitude",
                table: "Entries",
                schema: Schema.Name,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TrackLongitude",
                table: "Entries",
                schema: Schema.Name,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrackPointCount",
                table: "Entries",
                schema: Schema.Name,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrackAltitude",
                schema: Schema.Name,
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "TrackData",
                schema: Schema.Name,
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "TrackLatitude",
                schema: Schema.Name,
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "TrackLongitude",
                schema: Schema.Name,
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "TrackPointCount",
                schema: Schema.Name,
                table: "Entries");
        }
    }
}
