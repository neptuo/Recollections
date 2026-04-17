using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Neptuo.Recollections.Migrations;

#nullable disable

namespace Neptuo.Recollections.Accounts.Migrations
{
    /// <summary>
    /// The <see cref="Notifications"/> and <see cref="ImmediateNewEntriesDispatch"/> migrations were generated with
    /// the SQLite provider and therefore only contain the <c>Sqlite:Autoincrement</c> annotation on the identity
    /// columns. When the migrations are applied to SQL Server, the <c>Id</c> columns are created as plain
    /// <c>int NOT NULL</c> columns without the <c>IDENTITY</c> property, which causes inserts to fail with
    /// "Cannot insert the value NULL into column 'Id'". This migration drops and recreates those tables on SQL
    /// Server so the identity column is configured correctly. On SQLite, no action is necessary because the
    /// <c>INTEGER PRIMARY KEY AUTOINCREMENT</c> column is already generating values.
    /// </summary>
    [DbContext(typeof(DataContext))]
    [Migration("20260417214500_FixNotificationIdentityColumns")]
    public partial class FixNotificationIdentityColumns : MigrationWithSchema<DataContext>
    {
        private const string SqlServerProvider = "Microsoft.EntityFrameworkCore.SqlServer";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider != SqlServerProvider)
                return;

            migrationBuilder.DropTable(
                name: "PushSubscriptions",
                schema: Schema.Name);

            migrationBuilder.DropTable(
                name: "UserNotificationNewEntriesDispatches",
                schema: Schema.Name);

            migrationBuilder.CreateTable(
                name: "PushSubscriptions",
                schema: Schema.Name,
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(maxLength: 36, nullable: false),
                    Endpoint = table.Column<string>(maxLength: 800, nullable: false),
                    P256dh = table.Column<string>(nullable: false),
                    Auth = table.Column<string>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    LastSeenAt = table.Column<DateTime>(nullable: false),
                    RevokedAt = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PushSubscriptions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: Schema.Name,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_Endpoint",
                table: "PushSubscriptions",
                schema: Schema.Name,
                column: "Endpoint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_UserId",
                table: "PushSubscriptions",
                schema: Schema.Name,
                column: "UserId");

            migrationBuilder.CreateTable(
                name: "UserNotificationNewEntriesDispatches",
                schema: Schema.Name,
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider != SqlServerProvider)
                return;

            throw new NotSupportedException(
                "This migration cannot be rolled back on SQL Server because it fixes incorrectly generated non-identity Id columns. Reverting it would leave the database schema in a broken state.");
        }
    }
}
