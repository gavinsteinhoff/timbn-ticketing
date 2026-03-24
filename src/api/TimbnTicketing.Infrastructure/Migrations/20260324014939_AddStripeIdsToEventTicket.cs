using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimbnTicketing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeIdsToEventTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripePriceId",
                table: "EventTickets",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeProductId",
                table: "EventTickets",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripePriceId",
                table: "EventTickets");

            migrationBuilder.DropColumn(
                name: "StripeProductId",
                table: "EventTickets");
        }
    }
}
