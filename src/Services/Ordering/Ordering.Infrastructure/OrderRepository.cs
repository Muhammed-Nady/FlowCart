using FlowCart.BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Ordering.Domain;

namespace Ordering.Infrastructure;

public sealed class OrderRepository : Repository<Order, Guid, OrderingDbContext>, IOrderRepository
{
    public OrderRepository(OrderingDbContext context) : base(context)
    {
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(o => o.Items)
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<Order?> GetWithItemsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }
}
