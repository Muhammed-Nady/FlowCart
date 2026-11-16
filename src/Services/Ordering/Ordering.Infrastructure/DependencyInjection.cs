using FlowCart.BuildingBlocks.Application;
using FlowCart.BuildingBlocks.Domain;
using FlowCart.BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Application;
using Ordering.Domain;
using Polly;

namespace Ordering.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OrderingDb")
            ?? "Server=localhost,1433;Database=OrderingDb;User Id=sa;Password=Your_strong_pwd123!;TrustServerCertificate=True;";

        services.AddDbContext<OrderingDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            }));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork<OrderingDbContext>>();
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();

        // Resilient HTTP Client for Catalog Service
        var catalogUrl = configuration["Services:CatalogUrl"] ?? "http://localhost:5001/";
        services.AddHttpClient<ICatalogServiceClient, CatalogServiceClient>(client =>
        {
            client.BaseAddress = new Uri(catalogUrl);
        })
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(1);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
            options.CircuitBreaker.FailureRatio = 0.5;
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
        });

        // Resilient HTTP Client for Inventory Service
        var inventoryUrl = configuration["Services:InventoryUrl"] ?? "http://localhost:5003/";
        services.AddHttpClient<IInventoryServiceClient, InventoryServiceClient>(client =>
        {
            client.BaseAddress = new Uri(inventoryUrl);
        })
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(1);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
            options.CircuitBreaker.FailureRatio = 0.5;
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
