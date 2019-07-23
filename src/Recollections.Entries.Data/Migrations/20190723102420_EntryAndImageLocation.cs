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
                name: "Entries_Locations",
                columns: table => new
                {
                    EntryId = table.Column<string>(nullable: false),
                    Id = table.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
                    Longitude = table.Column<double>(nullable: true),
                    Latitude = table.Column<double>(nullable: true),
                    Altitude = table.Column<double>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entries_Locations", x => new { x.EntryId, x.Id });
                    table.ForeignKey(
                        name: "FK_Entries_Locations_Entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "Entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Entries_Locations");

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
