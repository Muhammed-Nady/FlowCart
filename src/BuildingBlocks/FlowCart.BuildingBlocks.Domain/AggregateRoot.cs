namespace FlowCart.BuildingBlocks.Domain;

/// <summary>
/// Base class for Aggregate Roots in the domain model.
/// Guarantees business invariants across the entire boundary of internal entities.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : notnull
{
    protected AggregateRoot() { }

    protected AggregateRoot(TId id) : base(id) { }
}
