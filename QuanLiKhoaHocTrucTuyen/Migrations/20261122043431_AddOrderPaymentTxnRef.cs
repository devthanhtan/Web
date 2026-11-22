using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiKhoaHocTrucTuyen.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPaymentTxnRef : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentTxnRef",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentTxnRef",
                table: "Orders",
                column: "PaymentTxnRef");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_PaymentTxnRef",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentTxnRef",
                table: "Orders");
        }
    }
}
