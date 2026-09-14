using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraceabilitySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSerialNumberDateRemoveUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_serial_numbers_serial_number_code",
                table: "serial_numbers");

            migrationBuilder.AddColumn<DateOnly>(
                name: "date",
                table: "serial_numbers",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateIndex(
                name: "ix_serial_numbers_serial_number_code",
                table: "serial_numbers",
                column: "serial_number_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_serial_numbers_serial_number_code",
                table: "serial_numbers");

            migrationBuilder.DropColumn(
                name: "date",
                table: "serial_numbers");

            migrationBuilder.CreateIndex(
                name: "ix_serial_numbers_serial_number_code",
                table: "serial_numbers",
                column: "serial_number_code",
                unique: true);
        }
    }
}
