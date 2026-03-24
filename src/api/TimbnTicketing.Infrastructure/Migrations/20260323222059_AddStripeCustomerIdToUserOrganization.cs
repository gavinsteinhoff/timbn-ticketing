using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimbnTicketing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeCustomerIdToUserOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "UserOrganizations",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "UserOrganizations");
        }
    }
}
