var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var gridpulseDb = postgres.AddDatabase("gridpulsedb");

var kafka = builder.AddKafka("kafka")
    .WithDataVolume()
    .WithKafkaUI();

var schemaRegistry = builder.AddContainer("schema-registry", "confluentinc/cp-schema-registry", "8.3.1")
    .WithEnvironment("SCHEMA_REGISTRY_HOST_NAME", "schema-registry")
    .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS", $"PLAINTEXT://{kafka.Resource.InternalEndpoint.Property(EndpointProperty.HostAndPort)}")
    .WithHttpEndpoint(targetPort: 8081, name: "http")
    .WaitFor(kafka);

var streetName = builder.AddParameter("meter-simulator-street-name");
var startingAddress = builder.AddParameter("meter-simulator-starting-address");
var buildingsPerSide = builder.AddParameter("meter-simulator-buildings-per-side");
var city = builder.AddParameter("meter-simulator-city");
var zipCode = builder.AddParameter("meter-simulator-zip-code");

var usageAggregation = builder.AddProject<Projects.GridPulse_UsageAggregation>("usage-aggregation")
    .WithReference(gridpulseDb)
    .WaitFor(gridpulseDb);

builder.AddProject<Projects.GridPulse_Billing>("billing")
    .WithReference(gridpulseDb)
    .WaitFor(gridpulseDb)
    .WithReference(usageAggregation)
    .WaitFor(usageAggregation);

builder.AddProject<Projects.GridPulse_MeterSimulator>("meter-simulator")
    .WithReference(usageAggregation)
    .WaitFor(usageAggregation)
    .WithEnvironment("MeterSimulator__StreetName", streetName)
    .WithEnvironment("MeterSimulator__StartingAddress", startingAddress)
    .WithEnvironment("MeterSimulator__BuildingsPerSide", buildingsPerSide)
    .WithEnvironment("MeterSimulator__City", city)
    .WithEnvironment("MeterSimulator__ZipCode", zipCode);

builder.Build().Run();
