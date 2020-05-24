using Microsoft.EntityFrameworkCore.Migrations;
using Neptuo.Recollections.Migrations;

namespace Neptuo.Recollections.Entries.Migrations
{
    public partial class EntryAndImageLocation : MigrationWithSchema<DataContext>
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Location_Altitude",
                table: "Images",
                schema: Schema.Name,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Location_Latitude",
                table: "Images",
                schema: Schema.Name,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Location_Longitude",
                table: "Images",
                schema: Schema.Name,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EntriesLocations",
                schema: Schema.Name,
                columns: table => new
                {
                    Order = table.Column<int>(nullable: false),
                    EntryId = table.Column<string>(maxLength: 36, nullable: false),
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
                        principalSchema: Schema.Name,
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntriesLocations",
                schema: Schema.Name);

            migrationBuilder.DropColumn(
                name: "Location_Altitude",
                table: "Images",
                schema: Schema.Name);

            migrationBuilder.DropColumn(
                name: "Location_Latitude",
                table: "Images",
                schema: Schema.Name);

            migrationBuilder.DropColumn(
                name: "Location_Longitude",
                table: "Images",
                schema: Schema.Name);
        }
    }
}
