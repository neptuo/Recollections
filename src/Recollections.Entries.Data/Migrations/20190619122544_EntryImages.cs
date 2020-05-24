using Microsoft.EntityFrameworkCore.Migrations;
using Neptuo.Recollections.Migrations;

namespace Neptuo.Recollections.Entries.Migrations
{
    public partial class EntryImages : MigrationWithSchema<DataContext>
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Images",
                schema: Schema.Name,
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 36, nullable: false),
                    EntryId = table.Column<string>(maxLength: 36, nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    FileName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Images_Entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "Entries",
                        principalSchema: Schema.Name,
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Images_EntryId",
                table: "Images",
                schema: Schema.Name,
                column: "EntryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Images",
                schema: Schema.Name);
        }
    }
}
