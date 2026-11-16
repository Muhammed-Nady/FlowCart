using FlowCart.BuildingBlocks.Domain;

namespace FlowCart.BuildingBlocks.Application;

/// <summary>
/// Dispatcher contract for publishing domain events to in-process handlers.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
