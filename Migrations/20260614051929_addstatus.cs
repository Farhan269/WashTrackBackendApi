using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wsahRecieveDelivary.Migrations
{
    /// <inheritdoc />
    public partial class addstatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "WorkOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "ShiftDate",
                table: "WashTransactions",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "WorkOrders");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ShiftDate",
                table: "WashTransactions",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");
        }
    }
}
