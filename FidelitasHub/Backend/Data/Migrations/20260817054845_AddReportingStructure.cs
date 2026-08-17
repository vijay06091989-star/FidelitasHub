using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FidelitasHub.Migrations
{
    /// <inheritdoc />
    public partial class AddReportingStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReportingManagerId",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReportingTeamLeaderId",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ReportingManagerId",
                table: "Employees",
                column: "ReportingManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ReportingTeamLeaderId",
                table: "Employees",
                column: "ReportingTeamLeaderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Employees_ReportingManagerId",
                table: "Employees",
                column: "ReportingManagerId",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Employees_ReportingTeamLeaderId",
                table: "Employees",
                column: "ReportingTeamLeaderId",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Employees_ReportingManagerId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Employees_ReportingTeamLeaderId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_ReportingManagerId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_ReportingTeamLeaderId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ReportingManagerId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ReportingTeamLeaderId",
                table: "Employees");
        }
    }
}
