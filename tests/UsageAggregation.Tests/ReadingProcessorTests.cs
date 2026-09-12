using Microsoft.EntityFrameworkCore;

namespace GridPulse.UsageAggregation.Tests;

public sealed class ReadingProcessorTests
{
    private static UsageAggregationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<UsageAggregationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new UsageAggregationDbContext(options);
    }

    [Fact]
    public async Task ProcessAsync_NewReading_ReturnsTrueAndPersistsIt()
    {
        await using var dbContext = CreateDbContext();
        var processor = new ReadingProcessor(dbContext);
        var request = new MeterReadingRequest("MTR-100-Elm St", DateTimeOffset.UtcNow, 2.5, Guid.NewGuid());

        var processed = await processor.ProcessAsync(request, CancellationToken.None);

        Assert.True(processed);
        Assert.Single(dbContext.ProcessedReadings);
    }

    [Fact]
    public async Task ProcessAsync_DuplicateReadingId_ReturnsFalseAndDoesNotReprocess()
    {
        await using var dbContext = CreateDbContext();
        var processor = new ReadingProcessor(dbContext);
        var request = new MeterReadingRequest("MTR-100-Elm St", DateTimeOffset.UtcNow, 2.5, Guid.NewGuid());

        await processor.ProcessAsync(request, CancellationToken.None);
        var processedAgain = await processor.ProcessAsync(request, CancellationToken.None);

        Assert.False(processedAgain);
        Assert.Single(dbContext.ProcessedReadings);
    }

    [Fact]
    public async Task ProcessAsync_TwoReadingsSameMeterSameHour_SumsIntoOneHourlyUsageRow()
    {
        await using var dbContext = CreateDbContext();
        var processor = new ReadingProcessor(dbContext);
        var timestamp = new DateTimeOffset(2026, 9, 12, 17, 15, 0, TimeSpan.Zero);

        await processor.ProcessAsync(new MeterReadingRequest("MTR-100-Elm St", timestamp, 2.5, Guid.NewGuid()), CancellationToken.None);
        await processor.ProcessAsync(new MeterReadingRequest("MTR-100-Elm St", timestamp.AddMinutes(20), 1.75, Guid.NewGuid()), CancellationToken.None);

        var hourlyUsage = Assert.Single(dbContext.HourlyUsages);
        Assert.Equal(4.25, hourlyUsage.TotalKwh);
    }

    [Fact]
    public async Task ProcessAsync_ReadingsInDifferentHours_CreatesSeparateHourlyUsageRows()
    {
        await using var dbContext = CreateDbContext();
        var processor = new ReadingProcessor(dbContext);
        var firstHour = new DateTimeOffset(2026, 9, 12, 17, 15, 0, TimeSpan.Zero);
        var secondHour = firstHour.AddHours(1);

        await processor.ProcessAsync(new MeterReadingRequest("MTR-100-Elm St", firstHour, 2.5, Guid.NewGuid()), CancellationToken.None);
        await processor.ProcessAsync(new MeterReadingRequest("MTR-100-Elm St", secondHour, 1.75, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(2, dbContext.HourlyUsages.Count());
    }
}
