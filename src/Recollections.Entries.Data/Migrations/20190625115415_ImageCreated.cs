using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Neptuo.Recollections.Migrations;

namespace Neptuo.Recollections.Entries.Migrations
{
    public partial class ImageCreated : MigrationWithSchema<DataContext>
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "Images",
                schema: Schema.Name,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "When",
                table: "Images",
                schema: Schema.Name,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Created",
                table: "Images",
                schema: Schema.Name);

            migrationBuilder.DropColumn(
                name: "When",
                table: "Images",
                schema: Schema.Name);
        }
    }
}
