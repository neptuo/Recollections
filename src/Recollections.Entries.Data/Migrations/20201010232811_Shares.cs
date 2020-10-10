using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Neptuo.Recollections.Migrations;

namespace Neptuo.Recollections.Entries.Migrations
{
    public partial class Shares : MigrationWithSchema<DataContext>
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Shares",
                schema: Schema.Name,
                columns: table => new
                {
                    UserId = table.Column<string>(maxLength: 36, nullable: true),
                    EntryId = table.Column<string>(maxLength: 36, nullable: true),
                    StoryId = table.Column<string>(maxLength: 36, nullable: true),
                    ProfileUserId = table.Column<string>(maxLength: 36, nullable: true),
                    Permission = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shares", x => new { x.UserId, x.EntryId, x.StoryId, x.ProfileUserId });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                schema: Schema.Name,
                name: "Shares");
        }
    }
}
