using Microsoft.EntityFrameworkCore.Migrations;

namespace Neptuo.Recollections.Entries.Migrations
{
    public partial class EntryAndImageLocation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Location_Altitude",
                table: "Images",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Location_Latitude",
                table: "Images",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Location_Longitude",
                table: "Images",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EntriesLocations",
                columns: table => new
                {
                    Order = table.Column<int>(nullable: false),
                    EntryId = table.Column<string>(nullable: false),
                    Longitude = table.Column<double>(nullable: true),
                    Latitude = table.Column<double>(nullable: true),
                    Altitude = table.Column<double>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntriesLocations", x => new { x.EntryId, x.Order });
                    table.ForeignKey(
                        name: "FK_EntriesLocations_Entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "Entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntriesLocations");

            migrationBuilder.DropColumn(
                name: "Location_Altitude",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "Location_Latitude",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "Location_Longitude",
                table: "Images");
        }
    }
}
