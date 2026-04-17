using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Neptuo.Recollections.Migrations;

#nullable disable

namespace Neptuo.Recollections.Accounts.Migrations
{
    /// <inheritdoc />
    public partial class ImmediateNewEntriesDispatch : MigrationWithSchema<DataContext>
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserNotificationNewEntriesDispatches",
                schema: Schema.Name,
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(maxLength: 36, nullable: false),
                    EntryId = table.Column<string>(maxLength: 36, nullable: false),
                    Created = table.Column<DateTime>(nullable: false),
                    SentAt = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationNewEntriesDispatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotificationNewEntriesDispatches_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: Schema.Name,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationNewEntriesDispatches_UserId_EntryId",
                table: "UserNotificationNewEntriesDispatches",
                schema: Schema.Name,
                columns: new[] { "UserId", "EntryId" },
                unique: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserNotificationNewEntriesDispatches",
                schema: Schema.Name);
        }
    }
}
