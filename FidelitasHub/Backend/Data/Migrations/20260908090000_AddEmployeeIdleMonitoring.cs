using System;
using FidelitasHub.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FidelitasHub.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260908090000_AddEmployeeIdleMonitoring")]
    public partial class AddEmployeeIdleMonitoring : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableIdleMonitoring",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EmployeeIdleSessions",
                columns: table => new
                {
                    EmployeeIdleSessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AttendanceId = table.Column<int>(type: "int", nullable: false),
                    IdleStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdleEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    ComputerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WindowsUserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeIdleSessions", x => x.EmployeeIdleSessionId);
                    table.ForeignKey(
                        name: "FK_EmployeeIdleSessions_Attendances_AttendanceId",
                        column: x => x.AttendanceId,
                        principalTable: "Attendances",
                        principalColumn: "AttendanceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeIdleSessions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeIdleSessions_AttendanceId_IdleEnd",
                table: "EmployeeIdleSessions",
                columns: new[] { "AttendanceId", "IdleEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeIdleSessions_EmployeeId",
                table: "EmployeeIdleSessions",
                column: "EmployeeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "EmployeeIdleSessions");

            migrationBuilder.DropColumn(
                name: "EnableIdleMonitoring",
                table: "Employees");
        }
    }
}
