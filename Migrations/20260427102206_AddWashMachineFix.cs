using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wsahRecieveDelivary.Migrations
{
    /// <inheritdoc />
    public partial class AddWashMachineFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WashPlan_PlanDate_Shift_PlantId_UnitId",
                table: "WashPlan");

            migrationBuilder.DropIndex(
                name: "IX_WashPlan_WorkOrderId",
                table: "WashPlan");

            migrationBuilder.DropIndex(
                name: "IX_WashMachine_MachineCode",
                table: "WashMachine");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "WashMachine");

            migrationBuilder.AlterColumn<string>(
                name: "MachineCode",
                table: "WashMachine",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_WashPlan_WorkOrderId_ProcessStageId_PlanDate_Shift_PlantId_UnitId",
                table: "WashPlan",
                columns: new[] { "WorkOrderId", "ProcessStageId", "PlanDate", "Shift", "PlantId", "UnitId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WashPlan_WorkOrderId_ProcessStageId_PlanDate_Shift_PlantId_UnitId",
                table: "WashPlan");

            migrationBuilder.AlterColumn<string>(
                name: "MachineCode",
                table: "WashMachine",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "WashMachine",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_WashPlan_PlanDate_Shift_PlantId_UnitId",
                table: "WashPlan",
                columns: new[] { "PlanDate", "Shift", "PlantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_WashPlan_WorkOrderId",
                table: "WashPlan",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WashMachine_MachineCode",
                table: "WashMachine",
                column: "MachineCode",
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
