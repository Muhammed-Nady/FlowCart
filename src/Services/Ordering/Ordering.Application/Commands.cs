using FlowCart.BuildingBlocks.Application;

namespace Ordering.Application;

public sealed record CreateOrderCommand(
    Guid CustomerId,
    AddressDto ShippingAddress,
    IReadOnlyList<CreateOrderItemRequest> Items) : ICommand<OrderDto>;

public sealed record ConfirmOrderCommand(Guid OrderId) : ICommand<OrderDto>;

public sealed record CancelOrderCommand(Guid OrderId, string Reason) : ICommand<OrderDto>;
