using FlowCart.BuildingBlocks.Domain;

namespace Ordering.Domain;

public sealed record OrderCreatedDomainEvent(
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    decimal TotalAmount) : DomainEvent;

public sealed record OrderConfirmedDomainEvent(
    Guid OrderId,
    string OrderNumber) : DomainEvent;

public sealed record OrderCanceledDomainEvent(
    Guid OrderId,
    string OrderNumber,
    string Reason) : DomainEvent;
