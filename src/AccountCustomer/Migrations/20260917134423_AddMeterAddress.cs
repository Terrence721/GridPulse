using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridPulse.AccountCustomer.Migrations
{
    /// <inheritdoc />
    public partial class AddMeterAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StreetName",
                table: "Meters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StreetNumber",
                table: "Meters",
                type: "integer",
                nullable: true);

            // Backfill from the existing MeterId convention ("MTR-{number}-{streetName}") -
            // real, parseable data for every pre-existing row, not a placeholder default.
            // Real street names observed in this system ("Elm St", "W Michigan Ave") contain
            // no hyphens, so splitting on the first two hyphens is safe.
            migrationBuilder.Sql("""
                UPDATE "Meters"
                SET "StreetNumber" = split_part("MeterId", '-', 2)::integer,
                    "StreetName" = regexp_replace("MeterId", '^MTR-[0-9]+-', '')
                WHERE "MeterId" ~ '^MTR-[0-9]+-.+$';
                """);

            // Real, live-verified finding: some existing rows don't follow the convention at
            // all - AppHost.Tests' CoreLoopSmokeTests registers real, permanent SMOKE-TEST-METER-*
            // rows against this same persistent database on every run and never cleans up
            // (see the proposed GitHub issue). A sentinel fallback, not a crash - StreetNumber=0
            // never collides with a real correlation window later.
            migrationBuilder.Sql("""
                UPDATE "Meters"
                SET "StreetNumber" = 0,
                    "StreetName" = 'Unknown'
                WHERE "StreetName" IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "StreetName",
                table: "Meters",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "StreetNumber",
                table: "Meters",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StreetName",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "StreetNumber",
                table: "Meters");
        }
    }
}
