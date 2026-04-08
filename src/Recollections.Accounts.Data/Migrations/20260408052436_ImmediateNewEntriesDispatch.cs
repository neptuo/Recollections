using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neptuo.Recollections.Accounts.Migrations
{
    /// <inheritdoc />
    public partial class ImmediateNewEntriesDispatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserNotificationNewEntriesDispatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    EntryId = table.Column<string>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationNewEntriesDispatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotificationNewEntriesDispatches_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationNewEntriesDispatches_UserId_EntryId",
                table: "UserNotificationNewEntriesDispatches",
                columns: new[] { "UserId", "EntryId" },
                unique: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserNotificationNewEntriesDispatches");
        }
    }
}
