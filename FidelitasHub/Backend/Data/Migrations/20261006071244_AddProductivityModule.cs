using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FidelitasHub.Migrations
{
    /// <inheritdoc />
    public partial class AddProductivityModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductivityProcesses",
                columns: table => new
                {
                    ProductivityProcessId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcessCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProcessName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductivityProcesses", x => x.ProductivityProcessId);
                    table.ForeignKey(
                        name: "FK_ProductivityProcesses_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductivityProcesses_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductivityActivities",
                columns: table => new
                {
                    ProductivityActivityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductivityProcessId = table.Column<int>(type: "int", nullable: false),
                    ActivityCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActivityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MeasurementMethod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DefaultTargetPerDay = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DefaultWeight = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductivityActivities", x => x.ProductivityActivityId);
                    table.ForeignKey(
                        name: "FK_ProductivityActivities_ProductivityProcesses_ProductivityProcessId",
                        column: x => x.ProductivityProcessId,
                        principalTable: "ProductivityProcesses",
                        principalColumn: "ProductivityProcessId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductivityAssignments",
                columns: table => new
                {
                    ProductivityAssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    ProductivityActivityId = table.Column<int>(type: "int", nullable: false),
                    TargetPerDay = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductivityAssignments", x => x.ProductivityAssignmentId);
                    table.ForeignKey(
                        name: "FK_ProductivityAssignments_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductivityAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductivityAssignments_ProductivityActivities_ProductivityActivityId",
                        column: x => x.ProductivityActivityId,
                        principalTable: "ProductivityActivities",
                        principalColumn: "ProductivityActivityId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductivityEntries",
                columns: table => new
                {
                    ProductivityEntryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    ProductivityActivityId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TargetPerDaySnapshot = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    WeightSnapshot = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    WeightedAchievement = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EnteredByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    EnteredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductivityEntries", x => x.ProductivityEntryId);
                    table.ForeignKey(
                        name: "FK_ProductivityEntries_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductivityEntries_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductivityEntries_Employees_EnteredByEmployeeId",
                        column: x => x.EnteredByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductivityEntries_ProductivityActivities_ProductivityActivityId",
                        column: x => x.ProductivityActivityId,
                        principalTable: "ProductivityActivities",
                        principalColumn: "ProductivityActivityId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductivityActivities_ProductivityProcessId_ActivityCode",
                table: "ProductivityActivities",
                columns: new[] { "ProductivityProcessId", "ActivityCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductivityAssignments_ClientId",
                table: "ProductivityAssignments",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductivityAssignments_EmployeeId_ClientId_ProductivityActivityId_EffectiveFrom",
                table: "ProductivityAssignments",
                columns: new[] { "EmployeeId", "ClientId", "ProductivityActivityId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductivityAssignments_ProductivityActivityId",
                table: "ProductivityAssignments",
                column: "ProductivityActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductivityEntries_ClientId",
                table: "ProductivityEntries",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductivityEntries_EmployeeId",
                table: "ProductivityEntries",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductivityEntries_EnteredByEmployeeId",
                table: "ProductivityEntries",
                column: "EnteredByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductivityEntries_ProductionDate_EmployeeId_ClientId_ProductivityActivityId",
                table: "ProductivityEntries",
                columns: new[] { "ProductionDate", "EmployeeId", "ClientId", "ProductivityActivityId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductivityEntries_ProductivityActivityId",
                table: "ProductivityEntries",
                column: "ProductivityActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductivityProcesses_ClientId_ProcessCode",
                table: "ProductivityProcesses",
                columns: new[] { "ClientId", "ProcessCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductivityProcesses_DepartmentId",
                table: "ProductivityProcesses",
                column: "DepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductivityAssignments");

            migrationBuilder.DropTable(
                name: "ProductivityEntries");

            migrationBuilder.DropTable(
                name: "ProductivityActivities");

            migrationBuilder.DropTable(
                name: "ProductivityProcesses");
        }
    }
}
