using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Neptuo.Recollections.Migrations;

#nullable disable

namespace Neptuo.Recollections.Accounts.Migrations
{
    /// <inheritdoc />
    public partial class BirthdayNotifications : MigrationWithSchema<DataContext>
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserNotificationBirthdaySettings",
                schema: Schema.Name,
                columns: table => new
                {
                    UserId = table.Column<string>(maxLength: 36, nullable: false),
                    IsEnabled = table.Column<bool>(nullable: false),
                    PreferredHour = table.Column<int>(nullable: false),
                    TimeZone = table.Column<string>(maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationBirthdaySettings", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserNotificationBirthdaySettings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: Schema.Name,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserNotificationBirthdayDispatches",
                schema: Schema.Name,
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(maxLength: 36, nullable: false),
                    Date = table.Column<DateTime>(nullable: false),
                    Created = table.Column<DateTime>(nullable: false),
                    SentAt = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationBirthdayDispatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotificationBirthdayDispatches_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: Schema.Name,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationBirthdayDispatches_UserId_Date",
                table: "UserNotificationBirthdayDispatches",
                schema: Schema.Name,
                columns: new[] { "UserId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserNotificationBirthdayDispatches",
                schema: Schema.Name);

            migrationBuilder.DropTable(
                name: "UserNotificationBirthdaySettings",
                schema: Schema.Name);
        }
    }
}
