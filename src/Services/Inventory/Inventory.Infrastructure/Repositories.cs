using FlowCart.BuildingBlocks.Infrastructure;
using Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure;

public sealed class InventoryRepository : Repository<InventoryItem, Guid, InventoryDbContext>, IInventoryRepository
{
    public InventoryRepository(InventoryDbContext context) : base(context)
    {
    }

    public async Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryItem>> GetByProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default)
    {
        var idList = productIds.Distinct().ToList();
        return await DbSet.Where(i => idList.Contains(i.ProductId)).ToListAsync(cancellationToken);
    }
}

public sealed class StockReservationRepository : Repository<StockReservation, Guid, InventoryDbContext>, IStockReservationRepository
{
    public StockReservationRepository(InventoryDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<StockReservation>> GetActiveByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(r => r.OrderId == orderId && r.Status == ReservationStatus.Active)
            .ToListAsync(cancellationToken);
    }
}
