using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wsahRecieveDelivary.Migrations
{
    /// <inheritdoc />
    public partial class WashplanAddQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "WashPlan");

            migrationBuilder.AddColumn<decimal>(
                name: "AdjustedTargetQty",
                table: "WashPlan",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseTargetQty",
                table: "WashPlan",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalTargetQty",
                table: "WashPlan",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Percentage",
                table: "WashPlan",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdjustedTargetQty",
                table: "WashPlan");

            migrationBuilder.DropColumn(
                name: "BaseTargetQty",
                table: "WashPlan");

            migrationBuilder.DropColumn(
                name: "FinalTargetQty",
                table: "WashPlan");

            migrationBuilder.DropColumn(
                name: "Percentage",
                table: "WashPlan");

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "WashPlan",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
