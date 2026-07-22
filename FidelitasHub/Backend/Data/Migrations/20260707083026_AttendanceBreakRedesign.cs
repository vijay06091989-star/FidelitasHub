using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FidelitasHub.Migrations
{
    /// <inheritdoc />
    public partial class AttendanceBreakRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BreakEnd",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "BreakStart",
                table: "Attendances");

            migrationBuilder.AddColumn<int>(
                name: "TotalBreakMinutes",
                table: "Attendances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AttendanceBreaks",
                columns: table => new
                {
                    BreakId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttendanceId = table.Column<int>(type: "int", nullable: false),
                    BreakStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BreakEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceBreaks", x => x.BreakId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceBreaks");

            migrationBuilder.DropColumn(
                name: "TotalBreakMinutes",
                table: "Attendances");

            migrationBuilder.AddColumn<DateTime>(
                name: "BreakEnd",
                table: "Attendances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BreakStart",
                table: "Attendances",
                type: "datetime2",
                nullable: true);
        }
    }
}
