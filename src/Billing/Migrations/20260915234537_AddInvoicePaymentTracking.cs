using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridPulse.Billing.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePaymentTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaidAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Invoices",
                type: "text",
                nullable: false,
                defaultValue: "Open");

            migrationBuilder.AddColumn<string>(
                name: "StripeCheckoutSessionId",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripePaymentIntentId",
                table: "Invoices",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "StripeCheckoutSessionId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "StripePaymentIntentId",
                table: "Invoices");
        }
    }
}
