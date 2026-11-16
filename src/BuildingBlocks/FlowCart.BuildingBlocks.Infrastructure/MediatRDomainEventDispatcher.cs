using FlowCart.BuildingBlocks.Application;
using FlowCart.BuildingBlocks.Domain;
using MediatR;

namespace FlowCart.BuildingBlocks.Infrastructure;

public sealed class MediatRDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher _publisher;

    public MediatRDomainEventDispatcher(IPublisher publisher)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    public async Task DispatchEventsAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            if (domainEvent is INotification directNotification)
            {
                await _publisher.Publish(directNotification, cancellationToken);
            }
            else
            {
                var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
                var notification = Activator.CreateInstance(notificationType, domainEvent);
                if (notification is not null)
                {
                    await _publisher.Publish(notification, cancellationToken);
                }
            }
        }
    }
}
