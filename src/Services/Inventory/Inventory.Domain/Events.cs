using FlowCart.BuildingBlocks.Domain;

namespace Inventory.Domain;

public sealed record StockReservedDomainEvent(
    Guid ProductId,
    Guid OrderId,
    int Quantity) : DomainEvent;

public sealed record StockReleasedDomainEvent(
    Guid ProductId,
    Guid OrderId,
    int Quantity) : DomainEvent;

public sealed record StockReplenishedDomainEvent(
    Guid ProductId,
    int AddedQuantity,
    int NewTotalQuantity) : DomainEvent;
