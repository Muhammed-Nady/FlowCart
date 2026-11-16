var builder = WebApplication.CreateBuilder(args);

// Add YARP reverse proxy and load routes/clusters from appsettings
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseRouting();

app.MapHealthChecks("/healthz");

// Forward all matched traffic through YARP
app.MapReverseProxy();

app.Run();
