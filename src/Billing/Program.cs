using GridPulse.Billing;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<BillingDbContext>("gridpulsedb");
builder.Services.Configure<RatePlanOptions>(
    builder.Configuration.GetSection(RatePlanOptions.SectionName));
builder.Services.AddHttpClient("usage-aggregation", client =>
{
    client.BaseAddress = new Uri("http://usage-aggregation");
});
builder.Services.AddScoped<InvoiceGenerator>();

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapPost("/invoices", async (GenerateInvoiceRequest request, InvoiceGenerator generator, CancellationToken cancellationToken) =>
{
    var invoice = await generator.GenerateAsync(request.MeterId, request.PeriodStart, request.PeriodEnd, request.RatePlanType, cancellationToken);
    return Results.Created($"/invoices/{invoice.Id}", invoice);
});

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
