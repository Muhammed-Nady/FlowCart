var builder = WebApplication.CreateBuilder(args);

// Add YARP reverse proxy and load routes/clusters from appsettings
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseRouting();

app.MapGet("/", () => Results.Ok(new
{
    Service = "FlowCart API Gateway",
    Status = "Online",
    Routes = new
    {
        Catalog = "/api/products",
        Categories = "/api/categories",
        Orders = "/api/orders",
        Inventory = "/api/inventory/products/{productId}"
    },
    HealthCheck = "/healthz",
    TimestampUtc = DateTime.UtcNow
}));

app.MapHealthChecks("/healthz");

// Forward all matched traffic through YARP
app.MapReverseProxy();

app.Run();
