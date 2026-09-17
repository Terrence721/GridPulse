using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using GridPulse.GridOperations;
using GridPulse.GridOperations.Avro;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<GridOperationsDbContext>("gridoperationsdb");

builder.Services.Configure<GridOperationsOptions>(
    builder.Configuration.GetSection(GridOperationsOptions.SectionName));
builder.Services.AddOptions<GridOperationsOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<WorkOrderStatusTransitioner>();
builder.Services.AddSingleton<OutageCorrelator>();
builder.Services.AddScoped<WorkOrderStatusUpdater>();
builder.Services.AddScoped<WorkOrderCorrelationProcessor>();

builder.Services.AddHttpClient("account-customer", client =>
{
    client.BaseAddress = new Uri("http://account-customer");
});

builder.Services.AddSingleton<ISchemaRegistryClient>(_ =>
{
    var schemaRegistryUrl = builder.Configuration["services:schema-registry:http:0"]
        ?? throw new InvalidOperationException("Schema registry endpoint not configured.");
    return new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = schemaRegistryUrl });
});

// Latest, not Earliest - a replayed historical anomaly for a meter that has since resumed
// transmitting would spawn a stale draft WorkOrder with nothing to ever resolve it, since
// there's no corresponding "meter came back online" event on this topic. Same reasoning as
// InvoicePaymentConsumer's AutoOffsetReset.Latest in Billing.
builder.AddKafkaConsumer<string, UsageAnomalyDetected>("kafka", settings =>
{
    settings.Config.GroupId = "grid-operations";
    settings.Config.AutoOffsetReset = AutoOffsetReset.Latest;
}, (sp, consumerBuilder) =>
{
    var schemaRegistry = sp.GetRequiredService<ISchemaRegistryClient>();
    consumerBuilder.SetValueDeserializer(new AvroDeserializer<UsageAnomalyDetected>(schemaRegistry).AsSyncOverAsync());
});
builder.Services.AddHostedService<UsageAnomalyConsumer>();

var app = builder.Build();
app.MapDefaultEndpoints();

try
{
    _ = app.Services.GetRequiredService<IOptions<GridOperationsOptions>>().Value;
}
catch (OptionsValidationException ex)
{
    foreach (var failure in ex.Failures)
    {
        Console.Error.WriteLine(failure);
    }

    return 1;
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GridOperationsDbContext>();
    dbContext.Database.Migrate();
}

app.MapPost("/work-orders", async (CreateWorkOrderRequest request, GridOperationsDbContext db, CancellationToken cancellationToken) =>
{
    if (!WorkOrderHazardTypes.All.Contains(request.HazardType))
    {
        return Results.BadRequest($"Unknown HazardType '{request.HazardType}'. Valid values: {string.Join(", ", WorkOrderHazardTypes.All)}.");
    }

    var workOrder = new WorkOrder
    {
        Id = Guid.NewGuid(),
        HazardType = request.HazardType,
        MeterId = request.MeterId,
        AccountId = request.AccountId,
        StreetName = request.StreetName,
        StreetNumber = request.StreetNumber,
        Description = request.Description,
        AssignedCrew = request.AssignedCrew,
        Status = "Reported",
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    db.WorkOrders.Add(workOrder);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/work-orders/{workOrder.Id}", workOrder);
});

app.MapGet("/work-orders", async (GridOperationsDbContext db, CancellationToken cancellationToken) =>
    await db.WorkOrders.ToListAsync(cancellationToken));

app.MapGet("/work-orders/{id:guid}", async (Guid id, GridOperationsDbContext db, CancellationToken cancellationToken) =>
    await db.WorkOrders.FindAsync([id], cancellationToken) is { } workOrder ? Results.Ok(workOrder) : Results.NotFound());

app.MapPatch("/work-orders/{id:guid}/status", async (Guid id, UpdateWorkOrderStatusRequest request, GridOperationsDbContext db, WorkOrderStatusUpdater updater, CancellationToken cancellationToken) =>
{
    var workOrder = await db.WorkOrders.FindAsync([id], cancellationToken);
    if (workOrder is null)
    {
        return Results.NotFound();
    }

    var updated = await updater.UpdateStatusAsync(workOrder, request.Status, cancellationToken);
    return updated ? Results.Ok(workOrder) : Results.BadRequest($"Cannot transition from '{workOrder.Status}' to '{request.Status}'.");
});

app.MapGet("/outages", async (GridOperationsDbContext db, CancellationToken cancellationToken) =>
    await db.Outages.ToListAsync(cancellationToken));

app.MapGet("/outages/{id:guid}", async (Guid id, GridOperationsDbContext db, CancellationToken cancellationToken) =>
    await db.Outages.FindAsync([id], cancellationToken) is { } outage ? Results.Ok(outage) : Results.NotFound());

app.Run();
return 0;
