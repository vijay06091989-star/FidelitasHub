using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FidelitasHub.Migrations
{
    /// <inheritdoc />
    public partial class LeaveApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Remarks",
                table: "LeaveApplications",
                newName: "TeamLeaderStatus");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeRemarks",
                table: "LeaveApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ManagerRemarks",
                table: "LeaveApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerStatus",
                table: "LeaveApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TeamLeaderRemarks",
                table: "LeaveApplications",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmployeeRemarks",
                table: "LeaveApplications");

            migrationBuilder.DropColumn(
                name: "ManagerRemarks",
                table: "LeaveApplications");

            migrationBuilder.DropColumn(
                name: "ManagerStatus",
                table: "LeaveApplications");

            migrationBuilder.DropColumn(
                name: "TeamLeaderRemarks",
                table: "LeaveApplications");

            migrationBuilder.RenameColumn(
                name: "TeamLeaderStatus",
                table: "LeaveApplications",
                newName: "Remarks");
        }
    }
}
