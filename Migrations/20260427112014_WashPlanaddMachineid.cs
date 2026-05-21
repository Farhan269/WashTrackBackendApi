using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wsahRecieveDelivary.Migrations
{
    /// <inheritdoc />
    public partial class WashPlanaddMachineid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WashPlan_WorkOrderId_ProcessStageId_PlanDate_Shift_PlantId_UnitId",
                table: "WashPlan");

            migrationBuilder.AddColumn<long>(
                name: "MachineId",
                table: "WashPlan",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_WashPlan_WorkOrderId_ProcessStageId_PlanDate_Shift_PlantId_UnitId_MachineId",
                table: "WashPlan",
                columns: new[] { "WorkOrderId", "ProcessStageId", "PlanDate", "Shift", "PlantId", "UnitId", "MachineId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WashPlan_WorkOrderId_ProcessStageId_PlanDate_Shift_PlantId_UnitId_MachineId",
                table: "WashPlan");

            migrationBuilder.DropColumn(
                name: "MachineId",
                table: "WashPlan");

            migrationBuilder.CreateIndex(
                name: "IX_WashPlan_WorkOrderId_ProcessStageId_PlanDate_Shift_PlantId_UnitId",
                table: "WashPlan",
                columns: new[] { "WorkOrderId", "ProcessStageId", "PlanDate", "Shift", "PlantId", "UnitId" });
        }
    }
}
