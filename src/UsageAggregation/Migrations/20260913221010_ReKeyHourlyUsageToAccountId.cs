using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridPulse.UsageAggregation.Migrations
{
    /// <inheritdoc />
    public partial class ReKeyHourlyUsageToAccountId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing HourlyUsages rows predate accounts existing at all (Phase 1/2 was
            // MeterId-keyed) — there is no valid AccountId for them, so a plain rename
            // would leave real meter-id strings masquerading as account ids. Clear them
            // out rather than silently carry forward data that would be wrong.
            migrationBuilder.Sql("""DELETE FROM "HourlyUsages";""");

            migrationBuilder.DropIndex(
                name: "IX_HourlyUsages_MeterId_PeriodStart",
                table: "HourlyUsages");

            migrationBuilder.DropColumn(
                name: "MeterId",
                table: "HourlyUsages");

            migrationBuilder.AddColumn<string>(
                name: "AccountId",
                table: "HourlyUsages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyUsages_AccountId_PeriodStart",
                table: "HourlyUsages",
                columns: new[] { "AccountId", "PeriodStart" },
                unique: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountId",
                table: "ProcessedReadings",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "ProcessedReadings");

            migrationBuilder.DropIndex(
                name: "IX_HourlyUsages_AccountId_PeriodStart",
                table: "HourlyUsages");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "HourlyUsages");

            migrationBuilder.AddColumn<string>(
                name: "MeterId",
                table: "HourlyUsages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyUsages_MeterId_PeriodStart",
                table: "HourlyUsages",
                columns: new[] { "MeterId", "PeriodStart" },
                unique: true);
        }
    }
}
