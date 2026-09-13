using GridPulse.AccountCustomer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AccountCustomerDbContext>("accountsdb");

var app = builder.Build();
app.MapDefaultEndpoints();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AccountCustomerDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
