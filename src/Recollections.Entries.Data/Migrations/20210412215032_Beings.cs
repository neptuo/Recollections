using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Neptuo.Recollections.Migrations;

namespace Neptuo.Recollections.Entries.Migrations
{
    public partial class Beings : MigrationWithSchema<DataContext>
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Beings",
                schema: Schema.Name,
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 36, nullable: false),
                    UserId = table.Column<string>(maxLength: 36, nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Icon = table.Column<string>(nullable: true),
                    Text = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BeingShares",
                schema: Schema.Name,
                columns: table => new
                {
                    UserId = table.Column<string>(maxLength: 36, nullable: false),
                    BeingId = table.Column<string>(maxLength: 36, nullable: false),
                    Permission = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeingShares", x => new { x.UserId, x.BeingId });
                });

            migrationBuilder.CreateTable(
                name: "BeingEntry",
                schema: Schema.Name,
                columns: table => new
                {
                    BeingsId = table.Column<string>(maxLength: 36, nullable: false),
                    EntriesId = table.Column<string>(maxLength: 36, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeingEntry", x => new { x.BeingsId, x.EntriesId });
                    table.ForeignKey(
                        name: "FK_BeingEntry_Beings_BeingsId",
                        column: x => x.BeingsId,
                        principalTable: "Beings",
                        principalSchema: Schema.Name,
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BeingEntry_Entries_EntriesId",
                        column: x => x.EntriesId,
                        principalTable: "Entries",
                        principalSchema: Schema.Name,
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BeingEntry_EntriesId",
                table: "BeingEntry",
                schema: Schema.Name,
                column: "EntriesId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                schema: Schema.Name,
                name: "BeingEntry");

            migrationBuilder.DropTable(
                schema: Schema.Name,
                name: "BeingShares");

            migrationBuilder.DropTable(
                schema: Schema.Name,
                name: "Beings");
        }
    }
}
