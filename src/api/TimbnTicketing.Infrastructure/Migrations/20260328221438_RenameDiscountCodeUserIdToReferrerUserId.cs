using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimbnTicketing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameDiscountCodeUserIdToReferrerUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscountCodes_Users_UserId",
                table: "DiscountCodes");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "DiscountCodes",
                newName: "ReferrerUserId");

            migrationBuilder.RenameIndex(
                name: "IX_DiscountCodes_UserId",
                table: "DiscountCodes",
                newName: "IX_DiscountCodes_ReferrerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscountCodes_Users_ReferrerUserId",
                table: "DiscountCodes",
                column: "ReferrerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscountCodes_Users_ReferrerUserId",
                table: "DiscountCodes");

            migrationBuilder.RenameColumn(
                name: "ReferrerUserId",
                table: "DiscountCodes",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_DiscountCodes_ReferrerUserId",
                table: "DiscountCodes",
                newName: "IX_DiscountCodes_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscountCodes_Users_UserId",
                table: "DiscountCodes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
