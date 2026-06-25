using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraceabilitySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProcessIdAddTypeToSerialNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_serial_numbers_processes_process_id",
                table: "serial_numbers");

            migrationBuilder.DropIndex(
                name: "ix_serial_numbers_process_id",
                table: "serial_numbers");

            migrationBuilder.DropColumn(
                name: "process_id",
                table: "serial_numbers");

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "serial_numbers",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "type",
                table: "serial_numbers");

            migrationBuilder.AddColumn<int>(
                name: "process_id",
                table: "serial_numbers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_serial_numbers_process_id",
                table: "serial_numbers",
                column: "process_id");

            migrationBuilder.AddForeignKey(
                name: "fk_serial_numbers_processes_process_id",
                table: "serial_numbers",
                column: "process_id",
                principalTable: "processes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
