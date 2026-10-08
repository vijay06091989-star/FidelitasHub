using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FidelitasHub.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeProductivityProcessClientOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductivityProcesses_ClientId_ProcessCode",
                table: "ProductivityProcesses");

            migrationBuilder.AlterColumn<int>(
                name: "ClientId",
                table: "ProductivityProcesses",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_ProductivityProcesses_ClientId_ProcessCode",
                table: "ProductivityProcesses",
                columns: new[] { "ClientId", "ProcessCode" },
                unique: true,
                filter: "[ClientId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductivityProcesses_ClientId_ProcessCode",
                table: "ProductivityProcesses");

            migrationBuilder.AlterColumn<int>(
                name: "ClientId",
                table: "ProductivityProcesses",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductivityProcesses_ClientId_ProcessCode",
                table: "ProductivityProcesses",
                columns: new[] { "ClientId", "ProcessCode" },
                unique: true);
        }
    }
}
