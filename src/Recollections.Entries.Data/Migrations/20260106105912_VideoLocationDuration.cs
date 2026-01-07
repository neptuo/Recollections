using Microsoft.EntityFrameworkCore.Migrations;
using Neptuo.Recollections.Migrations;

#nullable disable

namespace Neptuo.Recollections.Entries.Migrations
{
    /// <inheritdoc />
    public partial class VideoLocationDuration : MigrationWithSchema<DataContext>
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Duration",
                table: "Videos",
                schema: Schema.Name,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Location_Altitude",
                table: "Videos",
                schema: Schema.Name,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Location_Latitude",
                table: "Videos",
                schema: Schema.Name,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Location_Longitude",
                table: "Videos",
                schema: Schema.Name,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Videos",
                schema: Schema.Name);

            migrationBuilder.DropColumn(
                name: "Location_Altitude",
                table: "Videos",
                schema: Schema.Name);

            migrationBuilder.DropColumn(
                name: "Location_Latitude",
                table: "Videos",
                schema: Schema.Name);

            migrationBuilder.DropColumn(
                name: "Location_Longitude",
                table: "Videos",
                schema: Schema.Name);
        }
    }
}
