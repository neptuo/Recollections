using Microsoft.EntityFrameworkCore.Migrations;
using Neptuo.Recollections.Migrations;

#nullable disable

namespace Neptuo.Recollections.Entries.Migrations
{
    /// <inheritdoc />
    public partial class DropProfileShare : MigrationWithSchema<DataContext>
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                schema: Schema.Name,
                name: "ProfileShares");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfileShares",
                schema: Schema.Name,
                columns: table => new
                {
                    UserId = table.Column<string>(maxLength: 36, nullable: false),
                    ProfileId = table.Column<string>(maxLength: 36, nullable: false),
                    Permission = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileShares", x => new { x.UserId, x.ProfileId });
                });
        }
    }
}
