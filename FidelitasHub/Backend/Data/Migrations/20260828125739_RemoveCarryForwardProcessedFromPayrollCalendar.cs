using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FidelitasHub.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCarryForwardProcessedFromPayrollCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCarryForwardProcessed",
                table: "PayrollCalendars");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCarryForwardProcessed",
                table: "PayrollCalendars",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
