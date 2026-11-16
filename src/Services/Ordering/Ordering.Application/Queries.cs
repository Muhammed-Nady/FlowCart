using FlowCart.BuildingBlocks.Application;
using Ordering.Domain;

namespace Ordering.Application;

public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderDto>;

public sealed record GetCustomerOrdersQuery(Guid CustomerId) : IQuery<IReadOnlyList<OrderDto>>;

public sealed record GetOrdersPagedQuery(
    int PageNumber = 1,
    int PageSize = 10,
    OrderStatus? Status = null) : IQuery<PagedResult<OrderDto>>;
