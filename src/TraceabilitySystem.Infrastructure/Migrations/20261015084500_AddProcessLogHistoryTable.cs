using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraceabilitySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessLogHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "process_log_histories",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    process_log_id = table.Column<long>(type: "bigint", nullable: false),
                    serial_number_id = table.Column<int>(type: "int", nullable: false),
                    process_id = table.Column<int>(type: "int", nullable: false),
                    parameter_id = table.Column<int>(type: "int", nullable: false),
                    value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_process_log_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_process_log_histories_parameters_parameter_id",
                        column: x => x.parameter_id,
                        principalTable: "parameters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_process_log_histories_process_logs_process_log_id",
                        column: x => x.process_log_id,
                        principalTable: "process_logs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_process_log_histories_processes_process_id",
                        column: x => x.process_id,
                        principalTable: "processes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_process_log_histories_serial_numbers_serial_number_id",
                        column: x => x.serial_number_id,
                        principalTable: "serial_numbers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_process_log_histories_parameter_id",
                table: "process_log_histories",
                column: "parameter_id");

            migrationBuilder.CreateIndex(
                name: "ix_process_log_histories_process_id",
                table: "process_log_histories",
                column: "process_id");

            migrationBuilder.CreateIndex(
                name: "ix_process_log_histories_process_log_id",
                table: "process_log_histories",
                column: "process_log_id");

            migrationBuilder.CreateIndex(
                name: "ix_process_log_histories_serial_number_id",
                table: "process_log_histories",
                column: "serial_number_id");

            migrationBuilder.CreateIndex(
                name: "ix_process_log_histories_created_at",
                table: "process_log_histories",
                column: "created_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "process_log_histories");
        }
    }
}
