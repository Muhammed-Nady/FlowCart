using Inventory.API.Middlewares;
using Inventory.Application;
using Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<InventoryDbContext>("InventoryDb-HealthCheck");

// Clean Architecture layer registrations
builder.Services.AddInventoryApplication();
builder.Services.AddInventoryInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/healthz");

// Automatically apply schema and seed initial stock in development/container startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<InventoryDbContext>();
        if (context.Database.IsRelational())
        {
            await context.Database.EnsureCreatedAsync();

            if (!await context.InventoryItems.AnyAsync())
            {
                var item1 = Inventory.Domain.InventoryItem.Create(
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    50);

                var item2 = Inventory.Domain.InventoryItem.Create(
                    Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    100);

                var item3 = Inventory.Domain.InventoryItem.Create(
                    Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    75);

                await context.InventoryItems.AddRangeAsync(item1, item2, item3);
                await context.SaveChangesAsync();
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the Inventory database.");
    }
}

app.Run();

// Marker class for WebApplicationFactory in integration tests
public partial class Program { }
