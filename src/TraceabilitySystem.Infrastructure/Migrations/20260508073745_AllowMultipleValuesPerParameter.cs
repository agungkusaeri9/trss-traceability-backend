using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraceabilitySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleValuesPerParameter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "idx_process_parameter",
                table: "process_log_details",
                columns: new[] { "process_log_id", "process_id", "parameter_id" });

            migrationBuilder.DropIndex(
                name: "uk_process_parameter",
                table: "process_log_details");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_process_parameter",
                table: "process_log_details");

            migrationBuilder.CreateIndex(
                name: "uk_process_parameter",
                table: "process_log_details",
                columns: new[] { "process_log_id", "process_id", "parameter_id" },
                unique: true);
        }
    }
}
