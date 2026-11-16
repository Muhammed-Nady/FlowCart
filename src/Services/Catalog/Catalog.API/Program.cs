using Catalog.API.Middlewares;
using Catalog.Application;
using Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<CatalogDbContext>("CatalogDb-HealthCheck");

// Clean Architecture layer registrations
builder.Services.AddCatalogApplication();
builder.Services.AddCatalogInfrastructure(builder.Configuration);

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

// Automatically apply schema and seed data in development/container startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<CatalogDbContext>();
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();

            if (!await context.Categories.AnyAsync())
            {
                var electronics = new Catalog.Domain.Category(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    "Electronics",
                    "Consumer electronics and computer gear");

                var accessories = new Catalog.Domain.Category(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    "Accessories",
                    "Computer accessories and peripherals");

                await context.Categories.AddRangeAsync(electronics, accessories);

                var laptop = new Catalog.Domain.Product(
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    "Enterprise Dev Laptop 16-inch",
                    "High-performance developer workstation with 64GB RAM",
                    new Catalog.Domain.Sku("LAP-DEV-16"),
                    new Catalog.Domain.Money(1999.99m, "USD"),
                    electronics.Id);

                var mouse = new Catalog.Domain.Product(
                    Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    "Wireless Precision Mouse",
                    "Ergonomic wireless mouse with ultra-low latency",
                    new Catalog.Domain.Sku("MOU-WIR-01"),
                    new Catalog.Domain.Money(79.99m, "USD"),
                    accessories.Id);

                var keyboard = new Catalog.Domain.Product(
                    Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    "Mechanical Pro Keyboard",
                    "Hot-swappable mechanical keyboard with RGB backlighting",
                    new Catalog.Domain.Sku("KEY-PRO-01"),
                    new Catalog.Domain.Money(129.99m, "USD"),
                    accessories.Id);

                await context.Products.AddRangeAsync(laptop, mouse, keyboard);
                await context.SaveChangesAsync();
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the Catalog database.");
    }
}

app.Run();

// Marker class for WebApplicationFactory in integration tests
public partial class Program { }
