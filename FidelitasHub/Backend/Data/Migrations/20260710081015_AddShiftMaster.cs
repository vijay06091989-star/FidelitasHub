using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FidelitasHub.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoPunchOutTime",
                table: "Shifts");

            migrationBuilder.RenameColumn(
                name: "ShiftStart",
                table: "Shifts",
                newName: "StandardStartTime");

            migrationBuilder.RenameColumn(
                name: "ShiftEnd",
                table: "Shifts",
                newName: "StandardEndTime");

            migrationBuilder.RenameColumn(
                name: "GraceOutMinutes",
                table: "Shifts",
                newName: "MinimumPunchInMinutes");

            migrationBuilder.RenameColumn(
                name: "GraceInMinutes",
                table: "Shifts",
                newName: "MaximumPunchOutMinutes");

            migrationBuilder.AlterColumn<string>(
                name: "ShiftName",
                table: "Shifts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Shifts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "DstApplicable",
                table: "Shifts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "DstEndTime",
                table: "Shifts",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "DstStartTime",
                table: "Shifts",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GraceMinutes",
                table: "Shifts",
                type: "int",
                nullable: false,
                defaultValue: 15);

            migrationBuilder.AddColumn<int>(
                name: "MaximumBreakMinutes",
                table: "Shifts",
                type: "int",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.AddColumn<string>(
                name: "ShiftCode",
                table: "Shifts",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "DstApplicable",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "DstEndTime",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "DstStartTime",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "GraceMinutes",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "MaximumBreakMinutes",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "ShiftCode",
                table: "Shifts");

            migrationBuilder.RenameColumn(
                name: "StandardStartTime",
                table: "Shifts",
                newName: "ShiftStart");

            migrationBuilder.RenameColumn(
                name: "StandardEndTime",
                table: "Shifts",
                newName: "ShiftEnd");

            migrationBuilder.RenameColumn(
                name: "MinimumPunchInMinutes",
                table: "Shifts",
                newName: "GraceOutMinutes");

            migrationBuilder.RenameColumn(
                name: "MaximumPunchOutMinutes",
                table: "Shifts",
                newName: "GraceInMinutes");

            migrationBuilder.AlterColumn<string>(
                name: "ShiftName",
                table: "Shifts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "AutoPunchOutTime",
                table: "Shifts",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }
    }
}
