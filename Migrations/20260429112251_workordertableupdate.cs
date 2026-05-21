using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wsahRecieveDelivary.Migrations
{
    /// <inheritdoc />
    public partial class workordertableupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstWashBatchQty",
                table: "WorkOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstWashBatchTime",
                table: "WorkOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondWashBatchQty",
                table: "WorkOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondWashBatchTime",
                table: "WorkOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstWashBatchQty",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "FirstWashBatchTime",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "SecondWashBatchQty",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "SecondWashBatchTime",
                table: "WorkOrders");
        }
    }
}
