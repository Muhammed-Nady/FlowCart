using FlowCart.BuildingBlocks.Domain;
using MediatR;

namespace FlowCart.BuildingBlocks.Application;

public interface IDomainEventNotification : INotification
{
    IDomainEvent DomainEvent { get; }
}

public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : IDomainEventNotification
    where TDomainEvent : IDomainEvent
{
    IDomainEvent IDomainEventNotification.DomainEvent => DomainEvent;
}
