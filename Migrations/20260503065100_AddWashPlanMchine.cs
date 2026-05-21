using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wsahRecieveDelivary.Migrations
{
    /// <inheritdoc />
    public partial class AddWashPlanMchine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WashPlanMachine",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WashPlanId = table.Column<long>(type: "bigint", nullable: false),
                    MachineId = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WashPlanMachine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WashPlanMachine_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WashPlanMachine_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WashPlanMachine_WashMachine_MachineId",
                        column: x => x.MachineId,
                        principalTable: "WashMachine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WashPlanMachine_WashPlan_WashPlanId",
                        column: x => x.WashPlanId,
                        principalTable: "WashPlan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WashPlanMachine_CreatedBy",
                table: "WashPlanMachine",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WashPlanMachine_MachineId",
                table: "WashPlanMachine",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_WashPlanMachine_UpdatedBy",
                table: "WashPlanMachine",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WashPlanMachine_WashPlanId",
                table: "WashPlanMachine",
                column: "WashPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WashPlanMachine");
        }
    }
}
