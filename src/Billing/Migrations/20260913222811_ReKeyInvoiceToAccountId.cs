using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridPulse.Billing.Migrations
{
    /// <inheritdoc />
    public partial class ReKeyInvoiceToAccountId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing Invoices rows predate accounts existing at all — there is no valid
            // AccountId for them, so a plain rename would leave real meter-id strings
            // masquerading as account ids. Clear them out rather than silently carry
            // forward data that would be wrong. Same reasoning as
            // ReKeyHourlyUsageToAccountId in Usage Aggregation.
            migrationBuilder.Sql("""DELETE FROM "Invoices";""");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_MeterId_PeriodStart_PeriodEnd_RatePlanType",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "MeterId",
                table: "Invoices");

            migrationBuilder.AddColumn<string>(
                name: "AccountId",
                table: "Invoices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_AccountId_PeriodStart_PeriodEnd_RatePlanType",
                table: "Invoices",
                columns: new[] { "AccountId", "PeriodStart", "PeriodEnd", "RatePlanType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_AccountId_PeriodStart_PeriodEnd_RatePlanType",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Invoices");

            migrationBuilder.AddColumn<string>(
                name: "MeterId",
                table: "Invoices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_MeterId_PeriodStart_PeriodEnd_RatePlanType",
                table: "Invoices",
                columns: new[] { "MeterId", "PeriodStart", "PeriodEnd", "RatePlanType" },
                unique: true);
        }
    }
}
