using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neptuo.Recollections.Entries.Migrations
{
    /// <inheritdoc />
    public partial class VideoLocationDuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Duration",
                table: "Videos",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Location_Altitude",
                table: "Videos",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location_ImageId",
                table: "Videos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Location_Latitude",
                table: "Videos",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Location_Longitude",
                table: "Videos",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "Location_Altitude",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "Location_ImageId",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "Location_Latitude",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "Location_Longitude",
                table: "Videos");
        }
    }
}
