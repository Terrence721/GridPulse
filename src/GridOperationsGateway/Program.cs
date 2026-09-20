using GridPulse.GridOperationsGateway;

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

var app = builder.Build();
if (!app.Services.TryValidateStartupOptions<GridOperationsGatewayOptions>())
{
    return 1;
}

app.UseCors("GridOpsConsole");
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.Run();
return 0;

public partial class Program; // required for WebApplicationFactory<Program> in tests
