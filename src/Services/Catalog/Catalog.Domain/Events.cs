using FlowCart.BuildingBlocks.Domain;

namespace Catalog.Domain;

public sealed record ProductCreatedDomainEvent(
    Guid ProductId,
    string Name,
    string Sku,
    decimal Price,
    string Currency,
    Guid CategoryId) : DomainEvent;

public sealed record ProductPriceChangedDomainEvent(
    Guid ProductId,
    decimal OldPrice,
    decimal NewPrice,
    string Currency) : DomainEvent;
