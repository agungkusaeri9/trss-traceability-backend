using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraceabilitySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSerialNumberTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "serial_numbers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    serial_number_code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    process_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_serial_numbers", x => x.id);
                    table.ForeignKey(
                        name: "fk_serial_numbers_processes_process_id",
                        column: x => x.process_id,
                        principalTable: "processes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "serial_number_issues",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    serial_number_id = table.Column<int>(type: "int", nullable: false),
                    issue_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_serial_number_issues", x => x.id);
                    table.ForeignKey(
                        name: "fk_serial_number_issues_issues_issue_id",
                        column: x => x.issue_id,
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_serial_number_issues_serial_numbers_serial_number_id",
                        column: x => x.serial_number_id,
                        principalTable: "serial_numbers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "serial_number_relations",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    parent_serial_number_id = table.Column<int>(type: "int", nullable: false),
                    child_serial_number_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_serial_number_relations", x => x.id);
                    table.ForeignKey(
                        name: "fk_serial_number_relations_serial_numbers_child_serial_number_id",
                        column: x => x.child_serial_number_id,
                        principalTable: "serial_numbers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_serial_number_relations_serial_numbers_parent_serial_number_~",
                        column: x => x.parent_serial_number_id,
                        principalTable: "serial_numbers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_serial_number_issues_issue_id",
                table: "serial_number_issues",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_serial_number_issues_serial_number_id_issue_id",
                table: "serial_number_issues",
                columns: new[] { "serial_number_id", "issue_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_serial_number_relations_child_serial_number_id",
                table: "serial_number_relations",
                column: "child_serial_number_id");

            migrationBuilder.CreateIndex(
                name: "ix_serial_number_relations_parent_serial_number_id_child_serial~",
                table: "serial_number_relations",
                columns: new[] { "parent_serial_number_id", "child_serial_number_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_serial_numbers_process_id",
                table: "serial_numbers",
                column: "process_id");

            migrationBuilder.CreateIndex(
                name: "ix_serial_numbers_serial_number_code",
                table: "serial_numbers",
                column: "serial_number_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "serial_number_issues");

            migrationBuilder.DropTable(
                name: "serial_number_relations");

            migrationBuilder.DropTable(
                name: "serial_numbers");
        }
    }
}
