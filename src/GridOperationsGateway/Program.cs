using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using GridPulse.GridOperationsGateway;
using GridPulse.GridOperationsGateway.Avro;
using GridPulse.GridOperationsGateway.Contracts;
using GridPulse.GridOperationsGateway.Hubs;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddValidatedOptions<GridOperationsGatewayOptions>(GridOperationsGatewayOptions.SectionName);

// Read directly from config here, not from the validated options service -
// that's only available after Build(), but CORS policy setup needs to
// happen on the builder, before Build() is called.
var allowedCorsOrigins = builder.Configuration
    .GetSection($"{GridOperationsGatewayOptions.SectionName}:AllowedCorsOrigins")
    .Get<string[]>() ?? [];
builder.AddGatewayCors("GridOpsConsole", allowedCorsOrigins);
builder.AddGatewayAuthentication(audience: "grid-ops-api");

builder.Services.AddHttpClient("grid-operations", c => c.BaseAddress = new Uri("http://grid-operations"));
builder.Services.AddScoped<GridOperationsClient>();

builder.Services.AddSignalR();

builder.Services.AddSingleton<ISchemaRegistryClient>(_ =>
{
    var schemaRegistryUrl = builder.Configuration["services:schema-registry:http:0"]
        ?? throw new InvalidOperationException("Schema registry endpoint not configured.");
    return new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = schemaRegistryUrl });
});

// Own consumer group ("grid-operations-gateway"), independent of
// GridOperations' own "grid-operations" group - each group gets its own
// full copy of the topic. Latest, not Earliest, for the same reason as
// GridOperations' own consumer: no "meter came back online" event exists
// on this topic, so replaying history would flood a newly-connected
// dashboard session with stale "just went quiet" events.
builder.AddKafkaConsumer<string, UsageAnomalyDetected>("kafka", settings =>
{
    settings.Config.GroupId = "grid-operations-gateway";
    settings.Config.AutoOffsetReset = AutoOffsetReset.Latest;
    settings.DisableHealthChecks = true;
}, (sp, consumerBuilder) =>
{
    var schemaRegistry = sp.GetRequiredService<ISchemaRegistryClient>();
    consumerBuilder.SetValueDeserializer(new AvroDeserializer<UsageAnomalyDetected>(schemaRegistry).AsSyncOverAsync());
});
builder.Services.AddHostedService<AnomalyRelayConsumer>();

var app = builder.Build();
if (!app.Services.TryValidateStartupOptions<GridOperationsGatewayOptions>())
{
    return 1;
}

app.UseCors("GridOpsConsole");
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapHub<AnomalyFeedHub>("/hubs/anomaly-feed").RequireCors("GridOpsConsole");

var workOrders = app.MapGroup("/api/work-orders").RequireAuthorization();
workOrders.MapGet("", async (GridOperationsClient c, CancellationToken ct) => await c.GetWorkOrdersAsync(ct));
workOrders.MapGet("/{id:guid}", async (Guid id, GridOperationsClient c, CancellationToken ct) =>
{
    var wo = await c.GetWorkOrderAsync(id, ct);
    if (wo is null) return Results.NotFound();
    var outage = wo.OutageId is { } outageId ? await c.GetOutageAsync(outageId, ct) : null;
    return Results.Ok(new WorkOrderDetailDto(wo, outage));
});
workOrders.MapPost("", async (CreateWorkOrderRequest r, GridOperationsClient c, HttpContext ctx, CancellationToken ct) =>
{
    var resp = await c.CreateWorkOrderAsync(r, ct);
    ctx.Response.StatusCode = (int)resp.StatusCode;
    ctx.Response.ContentType = resp.Content.Headers.ContentType?.ToString() ?? "application/json";
    await resp.Content.CopyToAsync(ctx.Response.Body, ct);
});
workOrders.MapPatch("/{id:guid}/status", async (Guid id, UpdateWorkOrderStatusRequest r, GridOperationsClient c, HttpContext ctx, CancellationToken ct) =>
{
    var resp = await c.UpdateStatusAsync(id, r, ct);
    ctx.Response.StatusCode = (int)resp.StatusCode;
    ctx.Response.ContentType = resp.Content.Headers.ContentType?.ToString() ?? "application/json";
    await resp.Content.CopyToAsync(ctx.Response.Body, ct);
});

var outages = app.MapGroup("/api/outages").RequireAuthorization();
outages.MapGet("", async (GridOperationsClient c, CancellationToken ct) => await c.GetOutagesAsync(ct));
outages.MapGet("/{id:guid}", async (Guid id, GridOperationsClient c, CancellationToken ct) =>
    await c.GetOutageAsync(id, ct) is { } o ? Results.Ok(o) : Results.NotFound());

app.MapGet("/api/me", (ClaimsPrincipal user) =>
    Results.Ok(new { name = user.Identity?.Name, claims = user.Claims.Select(c => new { c.Type, c.Value }) }))
    .RequireAuthorization();

app.Run();
return 0;

public partial class Program; // required for WebApplicationFactory<Program> in tests
