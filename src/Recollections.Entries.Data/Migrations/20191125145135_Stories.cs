using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Neptuo.Recollections.Migrations;

namespace Neptuo.Recollections.Entries.Migrations
{
    public partial class Stories : MigrationWithSchema<DataContext>
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChapterId",
                table: "Entries",
                schema: Schema.Name,
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoryId",
                table: "Entries",
                schema: Schema.Name,
                maxLength: 36,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Stories",
                schema: Schema.Name,
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 36, nullable: false),
                    UserId = table.Column<string>(maxLength: 36, nullable: true),
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
                schema: Schema.Name,
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 36, nullable: false),
                    StoryId = table.Column<string>(maxLength: 36, nullable: true),
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
                        principalSchema: Schema.Name,
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entries_ChapterId",
                table: "Entries",
                schema: Schema.Name,
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Entries_StoryId",
                table: "Entries",
                schema: Schema.Name,
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryChapter_StoryId",
                table: "StoryChapter",
                schema: Schema.Name,
                column: "StoryId");

            if (!IsSqlite(migrationBuilder))
            {
                migrationBuilder.AddForeignKey(
                    name: "FK_Entries_StoryChapter_ChapterId",
                    table: "Entries",
                    schema: Schema.Name,
                    column: "ChapterId",
                    principalTable: "StoryChapter",
                    principalSchema: Schema.Name,
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);

                migrationBuilder.AddForeignKey(
                    name: "FK_Entries_Stories_StoryId",
                    table: "Entries",
                    schema: Schema.Name,
                    column: "StoryId",
                    principalTable: "Stories",
                    principalSchema: Schema.Name,
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
                    table: "Entries",
                    schema: Schema.Name);

                migrationBuilder.DropForeignKey(
                    name: "FK_Entries_Stories_StoryId",
                    table: "Entries",
                    schema: Schema.Name);
            }

            migrationBuilder.DropTable(
                name: "StoryChapter",
                schema: Schema.Name);

            migrationBuilder.DropTable(
                name: "Stories",
                schema: Schema.Name);

            migrationBuilder.DropIndex(
                name: "IX_Entries_ChapterId",
                table: "Entries",
                schema: Schema.Name);

            migrationBuilder.DropIndex(
                name: "IX_Entries_StoryId",
                table: "Entries",
                schema: Schema.Name);

            migrationBuilder.DropColumn(
                name: "ChapterId",
                table: "Entries",
                schema: Schema.Name);

            migrationBuilder.DropColumn(
                name: "StoryId",
                table: "Entries",
                schema: Schema.Name);
        }

        private bool IsSqlite(MigrationBuilder migrationBuilder)
            => migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite";
    }
}
