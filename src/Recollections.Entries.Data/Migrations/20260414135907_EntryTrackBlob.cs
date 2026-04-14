using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neptuo.Recollections.Entries.Migrations
{
    /// <inheritdoc />
    public partial class EntryTrackBlob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "TrackAltitude",
                table: "Entries",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackData",
                table: "Entries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TrackLatitude",
                table: "Entries",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TrackLongitude",
                table: "Entries",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrackPointCount",
                table: "Entries",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrackAltitude",
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "TrackData",
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "TrackLatitude",
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "TrackLongitude",
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "TrackPointCount",
                table: "Entries");
        }
    }
}
