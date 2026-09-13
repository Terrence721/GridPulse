var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var gridpulseDb = postgres.AddDatabase("gridpulsedb");
var accountsDb = postgres.AddDatabase("accountsdb");

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
    .WaitFor(schemaRegistry);

builder.AddProject<Projects.GridPulse_Billing>("billing")
    .WithReference(gridpulseDb)
    .WaitFor(gridpulseDb)
    .WithReference(kafka)
    .WaitFor(kafka)
    .WithReference(schemaRegistry.GetEndpoint("http"))
    .WaitFor(schemaRegistry);

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

builder.Build().Run();
