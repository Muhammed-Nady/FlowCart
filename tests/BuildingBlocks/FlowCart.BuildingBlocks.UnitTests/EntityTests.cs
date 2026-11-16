using FlowCart.BuildingBlocks.Domain;
using FluentAssertions;
using Xunit;

namespace FlowCart.BuildingBlocks.UnitTests;

public class EntityTests
{
    private record SampleDomainEvent(string Message) : DomainEvent;

    private class SampleAggregate : AggregateRoot<Guid>
    {
        public SampleAggregate(Guid id) : base(id) { }
    }

    [Fact]
    public void TwoEntities_WithSameId_ShouldBeEqual()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new SampleAggregate(id);
        var entity2 = new SampleAggregate(id);

        // Act & Assert
        entity1.Should().Be(entity2);
        (entity1 == entity2).Should().BeTrue();
        (entity1 != entity2).Should().BeFalse();
        entity1.GetHashCode().Should().Be(entity2.GetHashCode());
    }

    [Fact]
    public void TwoEntities_WithDifferentId_ShouldNotBeEqual()
    {
        // Arrange
        var entity1 = new SampleAggregate(Guid.NewGuid());
        var entity2 = new SampleAggregate(Guid.NewGuid());

        // Act & Assert
        entity1.Should().NotBe(entity2);
        (entity1 == entity2).Should().BeFalse();
        (entity1 != entity2).Should().BeTrue();
    }

    [Fact]
    public void AddDomainEvent_ShouldAppendToDomainEventsList()
    {
        // Arrange
        var entity = new SampleAggregate(Guid.NewGuid());
        var evt = new SampleDomainEvent("Test Event");

        // Act
        entity.AddDomainEvent(evt);

        // Assert
        entity.DomainEvents.Should().ContainSingle().Which.Should().Be(evt);
    }

    [Fact]
    public void ClearDomainEvents_ShouldEmptyTheDomainEventsList()
    {
        // Arrange
        var entity = new SampleAggregate(Guid.NewGuid());
        entity.AddDomainEvent(new SampleDomainEvent("Event 1"));
        entity.AddDomainEvent(new SampleDomainEvent("Event 2"));

        // Act
        entity.ClearDomainEvents();

        // Assert
        entity.DomainEvents.Should().BeEmpty();
    }
}
