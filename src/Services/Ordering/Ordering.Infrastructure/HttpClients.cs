using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ordering.Application;

namespace Ordering.Infrastructure;

public sealed class CatalogServiceClient : ICatalogServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogServiceClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CatalogServiceClient(HttpClient httpClient, ILogger<CatalogServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProductPriceValidationResult>> ValidatePricesAsync(
        IEnumerable<ProductPriceCheckRequest> items,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            Items = items.Select(i => new { ProductId = i.ProductId, ExpectedPrice = i.ExpectedPrice })
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/products/validate-prices", payload, cancellationToken);
            response.EnsureSuccessStatusCode();

            var results = await response.Content.ReadFromJsonAsync<List<ProductPriceValidationResult>>(JsonOptions, cancellationToken);
            return results ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate prices with Catalog Service.");
            throw;
        }
    }
}

public sealed class InventoryServiceClient : IInventoryServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryServiceClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public InventoryServiceClient(HttpClient httpClient, ILogger<InventoryServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ReserveStockResult> ReserveStockAsync(
        Guid orderId,
        IEnumerable<StockReservationItem> items,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            OrderId = orderId,
            Items = items.Select(i => new { ProductId = i.ProductId, Quantity = i.Quantity })
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/inventory/reserve", payload, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new ReserveStockResult(true, null);
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Inventory reservation failed for Order {OrderId}: {Error}", orderId, errorBody);
            return new ReserveStockResult(false, "Failed to reserve stock. Item(s) may be out of stock.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP call to Inventory Service failed for Order {OrderId}", orderId);
            return new ReserveStockResult(false, "Inventory service is temporarily unreachable.");
        }
    }

    public async Task ReleaseStockAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new { OrderId = orderId };
            var response = await _httpClient.PostAsJsonAsync("api/inventory/release", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Stock release failed for Order {OrderId} with status code {StatusCode}", orderId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while attempting to release stock for Order {OrderId}", orderId);
        }
    }
}
