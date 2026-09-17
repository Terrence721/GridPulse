using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridPulse.UsageAggregation.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessedReadingMeterTimestampIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProcessedReadings_MeterId_Timestamp",
                table: "ProcessedReadings",
                columns: new[] { "MeterId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProcessedReadings_MeterId_Timestamp",
                table: "ProcessedReadings");
        }
    }
}
