using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FidelitasHub.Migrations
{
    /// <inheritdoc />
    public partial class UpdateClientTeamLeaderStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clients_Employees_GeneralShiftTeamLeaderId",
                table: "Clients");

            migrationBuilder.RenameColumn(
                name: "GeneralShiftTeamLeaderId",
                table: "Clients",
                newName: "GeneralShiftPostingTeamLeaderId");

            migrationBuilder.RenameIndex(
                name: "IX_Clients_GeneralShiftTeamLeaderId",
                table: "Clients",
                newName: "IX_Clients_GeneralShiftPostingTeamLeaderId");

            migrationBuilder.AddColumn<int>(
                name: "GeneralShiftBillingTeamLeaderId",
                table: "Clients",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GeneralShiftDMTeamLeaderId",
                table: "Clients",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GeneralShiftEndToEndTeamLeaderId",
                table: "Clients",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_GeneralShiftBillingTeamLeaderId",
                table: "Clients",
                column: "GeneralShiftBillingTeamLeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_GeneralShiftDMTeamLeaderId",
                table: "Clients",
                column: "GeneralShiftDMTeamLeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_GeneralShiftEndToEndTeamLeaderId",
                table: "Clients",
                column: "GeneralShiftEndToEndTeamLeaderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Employees_GeneralShiftBillingTeamLeaderId",
                table: "Clients",
                column: "GeneralShiftBillingTeamLeaderId",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Employees_GeneralShiftDMTeamLeaderId",
                table: "Clients",
                column: "GeneralShiftDMTeamLeaderId",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Employees_GeneralShiftEndToEndTeamLeaderId",
                table: "Clients",
                column: "GeneralShiftEndToEndTeamLeaderId",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Employees_GeneralShiftPostingTeamLeaderId",
                table: "Clients",
                column: "GeneralShiftPostingTeamLeaderId",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clients_Employees_GeneralShiftBillingTeamLeaderId",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "FK_Clients_Employees_GeneralShiftDMTeamLeaderId",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "FK_Clients_Employees_GeneralShiftEndToEndTeamLeaderId",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "FK_Clients_Employees_GeneralShiftPostingTeamLeaderId",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Clients_GeneralShiftBillingTeamLeaderId",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Clients_GeneralShiftDMTeamLeaderId",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Clients_GeneralShiftEndToEndTeamLeaderId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "GeneralShiftBillingTeamLeaderId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "GeneralShiftDMTeamLeaderId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "GeneralShiftEndToEndTeamLeaderId",
                table: "Clients");

            migrationBuilder.RenameColumn(
                name: "GeneralShiftPostingTeamLeaderId",
                table: "Clients",
                newName: "GeneralShiftTeamLeaderId");

            migrationBuilder.RenameIndex(
                name: "IX_Clients_GeneralShiftPostingTeamLeaderId",
                table: "Clients",
                newName: "IX_Clients_GeneralShiftTeamLeaderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Employees_GeneralShiftTeamLeaderId",
                table: "Clients",
                column: "GeneralShiftTeamLeaderId",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
