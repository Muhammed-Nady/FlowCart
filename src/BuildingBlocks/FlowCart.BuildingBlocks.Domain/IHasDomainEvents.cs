namespace FlowCart.BuildingBlocks.Domain;

/// <summary>
/// Interface for entities that can produce and hold domain events until dispatched.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
