using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FidelitasHub.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductivityGroupsAndAllClientScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductivityGroups",
                columns: table => new
                {
                    ProductivityGroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductivityGroups", x => x.ProductivityGroupId);
                });

            migrationBuilder.CreateTable(
                name: "ProductivityGroupAssignments",
                columns: table => new
                {
                    ProductivityGroupAssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductivityGroupId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    ProductivityActivityId = table.Column<int>(type: "int", nullable: false),
                    TargetPerDay = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductivityGroupAssignments", x => x.ProductivityGroupAssignmentId);
                    table.ForeignKey(
                        name: "FK_ProductivityGroupAssignments_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductivityGroupAssignments_ProductivityActivities_ProductivityActivityId",
                        column: x => x.ProductivityActivityId,
                        principalTable: "ProductivityActivities",
                        principalColumn: "ProductivityActivityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductivityGroupAssignments_ProductivityGroups_ProductivityGroupId",
                        column: x => x.ProductivityGroupId,
                        principalTable: "ProductivityGroups",
                        principalColumn: "ProductivityGroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductivityGroupMembers",
                columns: table => new
                {
                    ProductivityGroupMemberId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductivityGroupId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductivityGroupMembers", x => x.ProductivityGroupMemberId);
                    table.ForeignKey(
                        name: "FK_ProductivityGroupMembers_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductivityGroupMembers_ProductivityGroups_ProductivityGroupId",
                        column: x => x.ProductivityGroupId,
                        principalTable: "ProductivityGroups",
                        principalColumn: "ProductivityGroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductivityGroupAssignments_ClientId",
                table: "ProductivityGroupAssignments",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductivityGroupAssignments_ProductivityActivityId",
                table: "ProductivityGroupAssignments",
                column: "ProductivityActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductivityGroupAssignments_ProductivityGroupId_ClientId_ProductivityActivityId_EffectiveFrom",
                table: "ProductivityGroupAssignments",
                columns: new[] { "ProductivityGroupId", "ClientId", "ProductivityActivityId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductivityGroupMembers_EmployeeId",
                table: "ProductivityGroupMembers",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductivityGroupMembers_ProductivityGroupId_EmployeeId_EffectiveFrom",
                table: "ProductivityGroupMembers",
                columns: new[] { "ProductivityGroupId", "EmployeeId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductivityGroups_GroupName",
                table: "ProductivityGroups",
                column: "GroupName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductivityGroupAssignments");

            migrationBuilder.DropTable(
                name: "ProductivityGroupMembers");

            migrationBuilder.DropTable(
                name: "ProductivityGroups");
        }
    }
}
