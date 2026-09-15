using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraceabilitySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTypeValueToParameter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "type_value",
                table: "parameters",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "value")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "type_value",
                table: "parameters");
        }
    }
}
