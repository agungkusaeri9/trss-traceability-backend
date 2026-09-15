using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraceabilitySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShowInDisplayToParameter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "show_in_display",
                table: "parameters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "show_in_display",
                table: "parameters");
        }
    }
}
