using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Neptuo.Recollections.Entries.Migrations
{
    public partial class Stories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChapterId",
                table: "Entries",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoryId",
                table: "Entries",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Stories",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    UserId = table.Column<string>(nullable: true),
                    Order = table.Column<int>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    Text = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoryChapter",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    StoryId = table.Column<string>(nullable: true),
                    Order = table.Column<int>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    Text = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryChapter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoryChapter_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entries_ChapterId",
                table: "Entries",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Entries_StoryId",
                table: "Entries",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryChapter_StoryId",
                table: "StoryChapter",
                column: "StoryId");

            if (!IsSqlite(migrationBuilder))
            {
                migrationBuilder.AddForeignKey(
                    name: "FK_Entries_StoryChapter_ChapterId",
                    table: "Entries",
                    column: "ChapterId",
                    principalTable: "StoryChapter",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);

                migrationBuilder.AddForeignKey(
                    name: "FK_Entries_Stories_StoryId",
                    table: "Entries",
                    column: "StoryId",
                    principalTable: "Stories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (!IsSqlite(migrationBuilder))
            {
                migrationBuilder.DropForeignKey(
                    name: "FK_Entries_StoryChapter_ChapterId",
                    table: "Entries");

                migrationBuilder.DropForeignKey(
                    name: "FK_Entries_Stories_StoryId",
                    table: "Entries");
            }

            migrationBuilder.DropTable(
                name: "StoryChapter");

            migrationBuilder.DropTable(
                name: "Stories");

            migrationBuilder.DropIndex(
                name: "IX_Entries_ChapterId",
                table: "Entries");

            migrationBuilder.DropIndex(
                name: "IX_Entries_StoryId",
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "ChapterId",
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "StoryId",
                table: "Entries");
        }

        private bool IsSqlite(MigrationBuilder migrationBuilder)
            => migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite";
    }
}
