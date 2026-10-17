using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerKey",
                table: "Receipts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ReceiptId",
                table: "ReceiptHistories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptHistories_ReceiptId",
                table: "ReceiptHistories",
                column: "ReceiptId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceiptHistories_Receipts_ReceiptId",
                table: "ReceiptHistories",
                column: "ReceiptId",
                principalTable: "Receipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReceiptHistories_Receipts_ReceiptId",
                table: "ReceiptHistories");

            migrationBuilder.DropIndex(
                name: "IX_ReceiptHistories_ReceiptId",
                table: "ReceiptHistories");

            migrationBuilder.DropColumn(
                name: "OwnerKey",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "ReceiptId",
                table: "ReceiptHistories");
        }
    }
}
