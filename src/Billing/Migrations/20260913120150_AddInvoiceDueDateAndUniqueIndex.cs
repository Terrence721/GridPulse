using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridPulse.Billing.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceDueDateAndUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DueDate",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            // Pre-existing invoices predate the upsert logic in InvoiceGenerator, so duplicate
            // (MeterId, PeriodStart, PeriodEnd, RatePlanType) rows can exist from repeated Phase 1
            // smoke-test runs. Keep the most recently generated row per key so CreateIndex below
            // doesn't fail against real data.
            migrationBuilder.Sql("""
                DELETE FROM "Invoices" a
                USING "Invoices" b
                WHERE a."MeterId" = b."MeterId"
                  AND a."PeriodStart" = b."PeriodStart"
                  AND a."PeriodEnd" = b."PeriodEnd"
                  AND a."RatePlanType" = b."RatePlanType"
                  AND (a."GeneratedAt", a.ctid) < (b."GeneratedAt", b.ctid);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_MeterId_PeriodStart_PeriodEnd_RatePlanType",
                table: "Invoices",
                columns: new[] { "MeterId", "PeriodStart", "PeriodEnd", "RatePlanType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_MeterId_PeriodStart_PeriodEnd_RatePlanType",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Invoices");
        }
    }
}
