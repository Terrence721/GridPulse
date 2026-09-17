using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GridPulse.UsageAggregation.Tests;

public sealed class UsageAnomalyProcessorTests
{
    private static UsageAggregationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<UsageAggregationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new UsageAggregationDbContext(options);
    }

    private static IOptions<UsageAnomalyOptions> CreateOptions(int expectedIntervalSeconds = 5, int missedIntervalMultiplier = 6) =>
        Options.Create(new UsageAnomalyOptions
        {
            ExpectedReadingIntervalSeconds = expectedIntervalSeconds,
            MissedIntervalMultiplier = missedIntervalMultiplier,
            SweepIntervalSeconds = 10
        });

    private static ProcessedReading CreateReading(string meterId, string accountId, DateTimeOffset timestamp) => new()
    {
        ReadingId = Guid.NewGuid(),
        MeterId = meterId,
        AccountId = accountId,
        Timestamp = timestamp,
        Kwh = 1.0,
        ReceivedAt = timestamp
    };

    [Fact]
    public async Task DetectAsync_MeterQuietPastThreshold_PublishesAnomaly()
    {
        await using var dbContext = CreateDbContext();
        dbContext.ProcessedReadings.Add(CreateReading("MTR-100-Elm St", "ACC-1", DateTimeOffset.UtcNow.AddSeconds(-40)));
        await dbContext.SaveChangesAsync();

        var publisher = new FakeUsageAnomalyPublisher();
        var processor = new UsageAnomalyProcessor(dbContext, publisher, CreateOptions());

        await processor.DetectAsync(CancellationToken.None);

        Assert.Single(publisher.PublishedReadings);
        Assert.Equal("MTR-100-Elm St", publisher.PublishedReadings[0].MeterId);
    }

    [Fact]
    public async Task DetectAsync_MeterRecentlySeen_DoesNotPublish()
    {
        await using var dbContext = CreateDbContext();
        dbContext.ProcessedReadings.Add(CreateReading("MTR-100-Elm St", "ACC-1", DateTimeOffset.UtcNow.AddSeconds(-5)));
        await dbContext.SaveChangesAsync();

        var publisher = new FakeUsageAnomalyPublisher();
        var processor = new UsageAnomalyProcessor(dbContext, publisher, CreateOptions());

        await processor.DetectAsync(CancellationToken.None);

        Assert.Empty(publisher.PublishedReadings);
    }

    [Fact]
    public async Task DetectAsync_MeterHasNewerReadingAfterOldOne_UsesLatestForThreshold()
    {
        await using var dbContext = CreateDbContext();
        dbContext.ProcessedReadings.Add(CreateReading("MTR-100-Elm St", "ACC-1", DateTimeOffset.UtcNow.AddMinutes(-5)));
        dbContext.ProcessedReadings.Add(CreateReading("MTR-100-Elm St", "ACC-1", DateTimeOffset.UtcNow.AddSeconds(-2)));
        await dbContext.SaveChangesAsync();

        var publisher = new FakeUsageAnomalyPublisher();
        var processor = new UsageAnomalyProcessor(dbContext, publisher, CreateOptions());

        await processor.DetectAsync(CancellationToken.None);

        Assert.Empty(publisher.PublishedReadings);
    }

    [Fact]
    public async Task DetectAsync_OneStaleOneFreshMeter_OnlyPublishesStaleOne()
    {
        await using var dbContext = CreateDbContext();
        dbContext.ProcessedReadings.Add(CreateReading("MTR-100-Elm St", "ACC-1", DateTimeOffset.UtcNow.AddSeconds(-40)));
        dbContext.ProcessedReadings.Add(CreateReading("MTR-200-Elm St", "ACC-2", DateTimeOffset.UtcNow.AddSeconds(-2)));
        await dbContext.SaveChangesAsync();

        var publisher = new FakeUsageAnomalyPublisher();
        var processor = new UsageAnomalyProcessor(dbContext, publisher, CreateOptions());

        await processor.DetectAsync(CancellationToken.None);

        var published = Assert.Single(publisher.PublishedReadings);
        Assert.Equal("MTR-100-Elm St", published.MeterId);
    }
}
