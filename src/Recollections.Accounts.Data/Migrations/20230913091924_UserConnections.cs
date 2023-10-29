using Microsoft.EntityFrameworkCore.Migrations;
using Neptuo.Recollections.Migrations;

#nullable disable

namespace Neptuo.Recollections.Accounts.Migrations
{
    /// <inheritdoc />
    public partial class UserConnections : MigrationWithSchema<DataContext>
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserConnections",
                schema: Schema.Name,
                columns: table => new
                {
                    UserId = table.Column<string>(maxLength: 36, nullable: false),
                    OtherUserId = table.Column<string>(maxLength: 36, nullable: false),
                    State = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConnections", x => new { x.UserId, x.OtherUserId });
                    table.ForeignKey(
                        name: "FK_UserConnections_AspNetUsers_OtherUserId",
                        column: x => x.OtherUserId,
                        principalTable: "AspNetUsers",
                        principalSchema: Schema.Name,
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_UserConnections_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalSchema: Schema.Name,
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserConnections_OtherUserId",
                table: "UserConnections",
                schema: Schema.Name,
                column: "OtherUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserConnections",
                schema: Schema.Name);
        }
    }
}
