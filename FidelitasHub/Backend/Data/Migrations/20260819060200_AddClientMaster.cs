using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FidelitasHub.Migrations
{
    /// <inheritdoc />
    public partial class AddClientMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    ClientId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ClientName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    GeneralShiftManagerId = table.Column<int>(type: "int", nullable: true),
                    GeneralShiftTeamLeaderId = table.Column<int>(type: "int", nullable: true),
                    USShiftManagerId = table.Column<int>(type: "int", nullable: true),
                    USShiftTeamLeaderId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.ClientId);
                    table.ForeignKey(
                        name: "FK_Clients_Employees_GeneralShiftManagerId",
                        column: x => x.GeneralShiftManagerId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Clients_Employees_GeneralShiftTeamLeaderId",
                        column: x => x.GeneralShiftTeamLeaderId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Clients_Employees_USShiftManagerId",
                        column: x => x.USShiftManagerId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Clients_Employees_USShiftTeamLeaderId",
                        column: x => x.USShiftTeamLeaderId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_GeneralShiftManagerId",
                table: "Clients",
                column: "GeneralShiftManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_GeneralShiftTeamLeaderId",
                table: "Clients",
                column: "GeneralShiftTeamLeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_USShiftManagerId",
                table: "Clients",
                column: "USShiftManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_USShiftTeamLeaderId",
                table: "Clients",
                column: "USShiftTeamLeaderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clients");
        }
    }
}
