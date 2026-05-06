using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraceabilitySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessParametersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "process_parameters",
                columns: table => new
                {
                    process_id = table.Column<int>(type: "int", nullable: false),
                    parameter_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_process_parameters", x => new { x.process_id, x.parameter_id });
                    table.ForeignKey(
                        name: "fk_process_parameters_parameters_parameter_id",
                        column: x => x.parameter_id,
                        principalTable: "parameters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_process_parameters_processes_process_id",
                        column: x => x.process_id,
                        principalTable: "processes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_process_parameters_parameter_id",
                table: "process_parameters",
                column: "parameter_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "process_parameters");
        }
    }
}
