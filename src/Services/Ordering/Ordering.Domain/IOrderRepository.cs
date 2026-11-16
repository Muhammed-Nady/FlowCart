using FlowCart.BuildingBlocks.Domain;

namespace Ordering.Domain;

public interface IOrderRepository : IRepository<Order, Guid>
{
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<Order?> GetWithItemsByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
