using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var gridpulseDb = postgres.AddDatabase("gridpulsedb");
var accountsDb = postgres.AddDatabase("accountsdb");
var gridOperationsDb = postgres.AddDatabase("gridoperationsdb");

var kafka = builder.AddKafka("kafka")
    .WithDataVolume()
    .WithKafkaUI();

var schemaRegistry = builder.AddContainer("schema-registry", "confluentinc/cp-schema-registry", "8.3.1")
    .WithEnvironment("SCHEMA_REGISTRY_HOST_NAME", "schema-registry")
    .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS", $"PLAINTEXT://{kafka.Resource.InternalEndpoint.Property(EndpointProperty.HostAndPort)}")
    .WithHttpEndpoint(targetPort: 8081, name: "http")
    .WithHttpHealthCheck("/subjects")
    .WaitFor(kafka);

var streetName = builder.AddParameter("meter-simulator-street-name");
var startingAddress = builder.AddParameter("meter-simulator-starting-address");
var buildingsPerSide = builder.AddParameter("meter-simulator-buildings-per-side");
var city = builder.AddParameter("meter-simulator-city");
var zipCode = builder.AddParameter("meter-simulator-zip-code");
var notificationWebhookUrl = builder.AddParameter("notification-webhook-url");
var stripeSecretKey = builder.AddParameter("stripe-secret-key", secret: true);
var stripeWebhookSecret = builder.AddParameter("stripe-webhook-secret", secret: true);

var accountCustomer = builder.AddProject<Projects.GridPulse_AccountCustomer>("account-customer")
    .WithReference(accountsDb)
    .WaitFor(accountsDb)
    .WithHttpHealthCheck(path: "/health", endpointName: "http")
    .WithEnvironment("AccountSeed__StreetName", streetName)
    .WithEnvironment("AccountSeed__StartingAddress", startingAddress)
    .WithEnvironment("AccountSeed__BuildingsPerSide", buildingsPerSide)
    .WithEnvironment("AccountSeed__NotificationWebhookUrl", notificationWebhookUrl);

builder.AddProject<Projects.GridPulse_UsageAggregation>("usage-aggregation")
    .WithReference(gridpulseDb)
    .WaitFor(gridpulseDb)
    .WithReference(kafka)
    .WaitFor(kafka)
    .WithReference(schemaRegistry.GetEndpoint("http"))
    .WaitFor(schemaRegistry)
    .WithHttpHealthCheck(path: "/health", endpointName: "http");

builder.AddProject<Projects.GridPulse_Billing>("billing")
    .WithReference(gridpulseDb)
    .WaitFor(gridpulseDb)
    .WithReference(kafka)
    .WaitFor(kafka)
    .WithReference(schemaRegistry.GetEndpoint("http"))
    .WaitFor(schemaRegistry)
    .WithHttpHealthCheck(path: "/health", endpointName: "http")
    .WithEnvironment("Stripe__SecretKey", stripeSecretKey)
    .WithEnvironment("Stripe__WebhookSigningSecret", stripeWebhookSecret);

builder.AddProject<Projects.GridPulse_GridOperations>("grid-operations")
    .WithReference(gridOperationsDb)
    .WaitFor(gridOperationsDb)
    .WithReference(kafka)
    .WaitFor(kafka)
    .WithReference(schemaRegistry.GetEndpoint("http"))
    .WaitFor(schemaRegistry)
    .WithReference(accountCustomer)
    .WaitFor(accountCustomer)
    .WithHttpHealthCheck(path: "/health", endpointName: "http");

builder.AddProject<Projects.GridPulse_MeterSimulator>("meter-simulator")
    .WithReference(kafka)
    .WaitFor(kafka)
    .WithReference(schemaRegistry.GetEndpoint("http"))
    .WaitFor(schemaRegistry)
    .WithReference(accountCustomer)
    .WaitFor(accountCustomer)
    .WithEnvironment("MeterSimulator__StreetName", streetName)
    .WithEnvironment("MeterSimulator__StartingAddress", startingAddress)
    .WithEnvironment("MeterSimulator__BuildingsPerSide", buildingsPerSide)
    .WithEnvironment("MeterSimulator__City", city)
    .WithEnvironment("MeterSimulator__ZipCode", zipCode);

builder.AddNodeApp("notification-service", "../notification-service", "src/index.ts")
    .WithYarn()
    .WithRunScript("start")
    .WithReference(kafka)
    .WaitFor(kafka)
    .WithReference(schemaRegistry.GetEndpoint("http"))
    .WaitFor(schemaRegistry)
    .WithReference(accountCustomer)
    .WaitFor(accountCustomer);

var appHostDirectory = builder.AppHostDirectory;
var app = builder.Build();

if (OperatingSystem.IsWindows())
{
    var stopScriptPath = Path.GetFullPath(Path.Combine(appHostDirectory, "..", "..", "scripts", "stop-app.ps1"));

    app.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(() =>
    {
        if (!File.Exists(stopScriptPath))
        {
            return;
        }

        // Runs on a graceful stop no matter how this AppHost was started (Visual
        // Studio, `dotnet run`, `aspire run`, or scripts/run-app.ps1) — not just
        // when stopped via the script, which only covers its own invocation.
        // A forceful kill (Task Manager, taskkill /F, a crash) bypasses this
        // entirely, the same as it would bypass any other in-process hook —
        // scripts/stop-app.ps1 remains available to run manually for that case.
        try
        {
            using var cleanup = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell",
                ArgumentList = { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", stopScriptPath },
                UseShellExecute = false,
                CreateNoWindow = true
            });
            cleanup?.WaitForExit(20_000);
        }
        catch (Exception ex)
        {
            // Best-effort cleanup — a failure here (e.g. powershell isn't
            // resolvable) shouldn't turn into an unhandled exception during
            // the host's own shutdown sequence.
            Console.Error.WriteLine($"GridPulse cleanup sweep failed to run: {ex.Message}");
        }
    });
}

app.Run();
