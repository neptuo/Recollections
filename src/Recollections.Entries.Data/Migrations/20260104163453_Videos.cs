using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Neptuo.Recollections.Migrations;

#nullable disable

namespace Neptuo.Recollections.Entries.Migrations
{
    /// <inheritdoc />
    public partial class Videos : MigrationWithSchema<DataContext>
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Videos",
                schema: Schema.Name,
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 36, nullable: false),
                    EntryId = table.Column<string>(maxLength: 36, nullable: true),
                    FileName = table.Column<string>(nullable: true),
                    ContentType = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    When = table.Column<DateTime>(nullable: false),
                    Created = table.Column<DateTime>(nullable: false),
                    OriginalWidth = table.Column<int>(nullable: false),
                    OriginalHeight = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Videos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Videos_Entries_EntryId",
                        column: x => x.EntryId,
                        principalSchema: Schema.Name,
                        principalTable: "Entries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Videos_EntryId",
                schema: Schema.Name,
                table: "Videos",
                column: "EntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Videos",
                schema: Schema.Name);
        }
    }
}
