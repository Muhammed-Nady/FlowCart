namespace Ordering.Application;

public interface ICatalogServiceClient
{
    Task<IReadOnlyList<ProductPriceValidationResult>> ValidatePricesAsync(
        IEnumerable<ProductPriceCheckRequest> items,
        CancellationToken cancellationToken = default);
}

public interface IInventoryServiceClient
{
    Task<ReserveStockResult> ReserveStockAsync(
        Guid orderId,
        IEnumerable<StockReservationItem> items,
        CancellationToken cancellationToken = default);

    Task ReleaseStockAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);
}
