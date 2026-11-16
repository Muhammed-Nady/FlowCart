using FlowCart.BuildingBlocks.Domain;

namespace Inventory.Domain;

public interface IInventoryRepository : IRepository<InventoryItem, Guid>
{
    Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryItem>> GetByProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default);
}

public interface IStockReservationRepository : IRepository<StockReservation, Guid>
{
    Task<IReadOnlyList<StockReservation>> GetActiveByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}
