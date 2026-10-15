using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraceabilitySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeProcessLogHistoryToProcessHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Rename table from process_log_histories to process_histories
            migrationBuilder.RenameTable(
                name: "process_log_histories",
                newName: "process_histories");

            // 2. Add columns: reference_type, reference_id, reference_code, traceability_log_id
            migrationBuilder.AddColumn<string>(
                name: "reference_type",
                table: "process_histories",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "ProcessLog")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "reference_id",
                table: "process_histories",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reference_code",
                table: "process_histories",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "traceability_log_id",
                table: "process_histories",
                type: "bigint",
                nullable: true);

            // 3. Alter columns: process_log_id and serial_number_id to nullable
            migrationBuilder.AlterColumn<long>(
                name: "process_log_id",
                table: "process_histories",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "serial_number_id",
                table: "process_histories",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            // 4. Add foreign key for traceability_log_id
            migrationBuilder.AddForeignKey(
                name: "fk_process_histories_traceability_logs_traceability_log_id",
                table: "process_histories",
                column: "traceability_log_id",
                principalTable: "traceability_logs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            // 5. Add indexes
            migrationBuilder.CreateIndex(
                name: "ix_process_histories_reference_type",
                table: "process_histories",
                column: "reference_type");

            migrationBuilder.CreateIndex(
                name: "ix_process_histories_reference_id",
                table: "process_histories",
                column: "reference_id");

            migrationBuilder.CreateIndex(
                name: "ix_process_histories_reference_code",
                table: "process_histories",
                column: "reference_code");

            migrationBuilder.CreateIndex(
                name: "ix_process_histories_traceability_log_id",
                table: "process_histories",
                column: "traceability_log_id");

            // 6. Backfill existing data
            migrationBuilder.Sql(@"
                UPDATE process_histories 
                SET reference_type = 'ProcessLog', 
                    reference_id = process_log_id 
                WHERE reference_type IS NULL OR reference_type = '' OR reference_type = 'ProcessLog';
            ");

            migrationBuilder.Sql(@"
                UPDATE process_histories ph 
                JOIN serial_numbers sn ON ph.serial_number_id = sn.id 
                SET ph.reference_code = sn.serial_number_code 
                WHERE ph.reference_code IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_process_histories_traceability_logs_traceability_log_id",
                table: "process_histories");

            migrationBuilder.DropIndex(
                name: "ix_process_histories_reference_type",
                table: "process_histories");

            migrationBuilder.DropIndex(
                name: "ix_process_histories_reference_id",
                table: "process_histories");

            migrationBuilder.DropIndex(
                name: "ix_process_histories_reference_code",
                table: "process_histories");

            migrationBuilder.DropIndex(
                name: "ix_process_histories_traceability_log_id",
                table: "process_histories");

            migrationBuilder.AlterColumn<int>(
                name: "serial_number_id",
                table: "process_histories",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "process_log_id",
                table: "process_histories",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "reference_type",
                table: "process_histories");

            migrationBuilder.DropColumn(
                name: "reference_id",
                table: "process_histories");

            migrationBuilder.DropColumn(
                name: "reference_code",
                table: "process_histories");

            migrationBuilder.DropColumn(
                name: "traceability_log_id",
                table: "process_histories");

            migrationBuilder.RenameTable(
                name: "process_histories",
                newName: "process_log_histories");
        }
    }
}
