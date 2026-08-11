using Microsoft.EntityFrameworkCore.Migrations;
using Neptuo.Recollections.Migrations;

#nullable disable

namespace Neptuo.Recollections.Entries.Migrations
{
    /// <inheritdoc />
    public partial class BeingNameDay : MigrationWithSchema<DataContext>
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NameDayMonth",
                table: "Beings",
                schema: Schema.Name,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NameDayDay",
                table: "Beings",
                schema: Schema.Name,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameDayMonth",
                schema: Schema.Name,
                table: "Beings");

            migrationBuilder.DropColumn(
                name: "NameDayDay",
                schema: Schema.Name,
                table: "Beings");
        }
    }
}
