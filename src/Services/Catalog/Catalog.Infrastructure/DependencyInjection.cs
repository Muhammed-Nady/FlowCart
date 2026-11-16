using Catalog.Application;
using Catalog.Domain;
using FlowCart.BuildingBlocks.Application;
using FlowCart.BuildingBlocks.Domain;
using FlowCart.BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CatalogDb")
            ?? "Server=localhost,1433;Database=CatalogDb;User Id=sa;Password=Your_strong_pwd123!;TrustServerCertificate=True;";

        services.AddDbContext<CatalogDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            }));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork<CatalogDbContext>>();
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();

        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? "localhost:6379";

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "Catalog:";
        });

        services.AddSingleton<ICatalogCacheService, RedisCatalogCacheService>();

        return services;
    }
}
