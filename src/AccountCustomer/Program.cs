using GridPulse.AccountCustomer;
using GridPulse.Shared;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AccountCustomerDbContext>("accountsdb");
builder.AddValidatedOptions<AccountSeedOptions>(AccountSeedOptions.SectionName);
builder.Services.AddSingleton<CityBlockMeterIdFactory>();
builder.Services.AddScoped<AccountSeeder>();

var app = builder.Build();
app.MapDefaultEndpoints();

if (!app.Services.TryValidateStartupOptions<AccountSeedOptions>())
{
    return 1;
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AccountCustomerDbContext>();
    dbContext.Database.Migrate();

    var seeder = scope.ServiceProvider.GetRequiredService<AccountSeeder>();
    await seeder.SeedAsync(CancellationToken.None);
}

app.MapPost("/accounts", async (CreateAccountRequest request, AccountCustomerDbContext db, CancellationToken cancellationToken) =>
{
    var account = new Account { Id = Guid.NewGuid(), ContactWebhookUrl = request.ContactWebhookUrl };
    db.Accounts.Add(account);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/accounts/{account.Id}", account);
});

app.MapGet("/accounts", async (AccountCustomerDbContext db, CancellationToken cancellationToken) =>
    await db.Accounts.ToListAsync(cancellationToken));

app.MapGet("/accounts/{id:guid}", async (Guid id, AccountCustomerDbContext db, CancellationToken cancellationToken) =>
    await db.Accounts.FindAsync([id], cancellationToken) is { } account ? Results.Ok(account) : Results.NotFound());

app.MapPut("/accounts/{id:guid}", async (Guid id, CreateAccountRequest request, AccountCustomerDbContext db, CancellationToken cancellationToken) =>
{
    var account = await db.Accounts.FindAsync([id], cancellationToken);
    if (account is null)
    {
        return Results.NotFound();
    }

    account.ContactWebhookUrl = request.ContactWebhookUrl;
    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok(account);
});

app.MapDelete("/accounts/{id:guid}", async (Guid id, AccountCustomerDbContext db, CancellationToken cancellationToken) =>
{
    var account = await db.Accounts.FindAsync([id], cancellationToken);
    if (account is null)
    {
        return Results.NotFound();
    }

    db.Accounts.Remove(account);
    await db.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
});

app.MapPost("/meters", async (RegisterMeterRequest request, AccountCustomerDbContext db, CancellationToken cancellationToken) =>
{
    var accountExists = await db.Accounts.AnyAsync(a => a.Id == request.AccountId, cancellationToken);
    if (!accountExists)
    {
        return Results.BadRequest($"No account with id '{request.AccountId}' exists.");
    }

    var meter = new Meter { MeterId = request.MeterId, AccountId = request.AccountId, StreetName = request.StreetName, StreetNumber = request.StreetNumber };
    db.Meters.Add(meter);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/meters/{meter.MeterId}", meter);
});

app.MapGet("/meters", async (AccountCustomerDbContext db, CancellationToken cancellationToken) =>
    await db.Meters.ToListAsync(cancellationToken));

app.MapGet("/meters/{meterId}", async (string meterId, AccountCustomerDbContext db, CancellationToken cancellationToken) =>
    await db.Meters.FindAsync([meterId], cancellationToken) is { } meter ? Results.Ok(meter) : Results.NotFound());

app.MapDelete("/meters/{meterId}", async (string meterId, AccountCustomerDbContext db, CancellationToken cancellationToken) =>
{
    var meter = await db.Meters.FindAsync([meterId], cancellationToken);
    if (meter is null)
    {
        return Results.NotFound();
    }

    db.Meters.Remove(meter);
    await db.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
});

app.Run();
return 0;
