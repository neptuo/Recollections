using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Neptuo;
using Neptuo.Recollections.Migrations;

namespace Neptuo.Recollections.Entries.Migrations
{
    public partial class Initial : MigrationWithSchema<DataContext>
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Entries",
                schema: Schema.Name,
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 36, nullable: false),
                    UserId = table.Column<string>(maxLength: 36, nullable: true),
                    Title = table.Column<string>(nullable: true),
                    Text = table.Column<string>(nullable: true),
                    When = table.Column<DateTime>(nullable: false),
                    Created = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entries", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Entries",
                schema: Schema.Name);
        }
    }
}
