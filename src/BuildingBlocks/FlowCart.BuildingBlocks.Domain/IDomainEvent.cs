namespace FlowCart.BuildingBlocks.Domain;

/// <summary>
/// Marker contract for domain events representing business facts that occurred in an Aggregate Root.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOnUtc { get; }
}

/// <summary>
/// Base record for domain events with automated ID and timestamp generation.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
