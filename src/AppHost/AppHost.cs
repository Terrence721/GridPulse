var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var gridpulseDb = postgres.AddDatabase("gridpulsedb");

var streetName = builder.AddParameter("meter-simulator-street-name");
var startingAddress = builder.AddParameter("meter-simulator-starting-address");
var buildingsPerSide = builder.AddParameter("meter-simulator-buildings-per-side");

builder.AddProject<Projects.GridPulse_MeterSimulator>("meter-simulator")
    .WithEnvironment("MeterSimulator__StreetName", streetName)
    .WithEnvironment("MeterSimulator__StartingAddress", startingAddress)
    .WithEnvironment("MeterSimulator__BuildingsPerSide", buildingsPerSide);

builder.AddProject<Projects.GridPulse_UsageAggregation>("usage-aggregation")
    .WithReference(gridpulseDb)
    .WaitFor(gridpulseDb);

builder.Build().Run();
