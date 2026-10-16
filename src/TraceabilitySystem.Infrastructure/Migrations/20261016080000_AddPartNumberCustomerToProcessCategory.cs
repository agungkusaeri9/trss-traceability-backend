using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraceabilitySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPartNumberCustomerToProcessCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "part_number_customer",
                table: "process_categories",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            // Backfill initial PartNumberCustomer data
            migrationBuilder.Sql("UPDATE process_categories SET part_number_customer = '21400C000P' WHERE LOWER(name) = 'clinching';");
            migrationBuilder.Sql("UPDATE process_categories SET part_number_customer = '21580C000P' WHERE LOWER(name) = 'mfan';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "part_number_customer",
                table: "process_categories");
        }
    }
}
