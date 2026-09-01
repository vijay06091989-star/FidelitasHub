using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FidelitasHub.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveBalancePayrollPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BalancePeriodEnd",
                table: "EmployeeLeaveBalances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BalancePeriodStart",
                table: "EmployeeLeaveBalances",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BalancePeriodEnd",
                table: "EmployeeLeaveBalances");

            migrationBuilder.DropColumn(
                name: "BalancePeriodStart",
                table: "EmployeeLeaveBalances");
        }
    }
}
