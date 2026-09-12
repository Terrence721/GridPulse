var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var gridpulseDb = postgres.AddDatabase("gridpulsedb");

builder.AddProject<Projects.GridPulse_MeterSimulator>("meter-simulator");

builder.Build().Run();
