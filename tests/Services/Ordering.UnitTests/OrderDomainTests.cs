using FluentAssertions;
using Ordering.Domain;
using Xunit;

namespace Ordering.UnitTests;

public class OrderDomainTests
{
    private static Address CreateSampleAddress() =>
        new("123 Tech Way", "Redmond", "WA", "USA", "98052");

    [Fact]
    public void CreateOrder_ShouldInitializeInPendingStatusAndRaiseDomainEvent()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var address = CreateSampleAddress();

        // Act
        var order = Order.Create("ORD-001", customerId, address);

        // Assert
        order.Id.Should().NotBeEmpty();
        order.OrderNumber.Should().Be("ORD-001");
        order.CustomerId.Should().Be(customerId);
        order.Status.Should().Be(OrderStatus.Pending);
        order.Items.Should().BeEmpty();
        order.TotalAmount.Should().Be(0m);

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderCreatedDomainEvent>();
    }

    [Fact]
    public void AddItem_WhenPending_ShouldCalculateTotalCorrectly()
    {
        // Arrange
        var order = Order.Create("ORD-002", Guid.NewGuid(), CreateSampleAddress());

        // Act
        order.AddItem(Guid.NewGuid(), "Mechanical Keyboard", 100m, 2);
        order.AddItem(Guid.NewGuid(), "Gaming Mouse", 50m, 1);

        // Assert
        order.Items.Should().HaveCount(2);
        order.TotalAmount.Should().Be(250m);
    }

    [Fact]
    public void AddItem_ForSameProduct_ShouldAccumulateQuantity()
    {
        // Arrange
        var order = Order.Create("ORD-003", Guid.NewGuid(), CreateSampleAddress());
        var productId = Guid.NewGuid();

        // Act
        order.AddItem(productId, "USB-C Cable", 15m, 2);
        order.AddItem(productId, "USB-C Cable", 15m, 3);

        // Assert
        order.Items.Should().ContainSingle();
        order.Items.First().Quantity.Should().Be(5);
        order.TotalAmount.Should().Be(75m);
    }

    [Fact]
    public void Confirm_WhenPendingWithItems_ShouldTransitionToConfirmed()
    {
        // Arrange
        var order = Order.Create("ORD-004", Guid.NewGuid(), CreateSampleAddress());
        order.AddItem(Guid.NewGuid(), "Monitor", 300m, 1);
        order.ClearDomainEvents();

        // Act
        order.Confirm();

        // Assert
        order.Status.Should().Be(OrderStatus.Confirmed);
        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderConfirmedDomainEvent>();
    }

    [Fact]
    public void Confirm_WhenEmptyItems_ShouldThrowOrderingDomainException()
    {
        // Arrange
        var order = Order.Create("ORD-005", Guid.NewGuid(), CreateSampleAddress());

        // Act
        var act = () => order.Confirm();

        // Assert
        act.Should().Throw<OrderingDomainException>()
            .WithMessage("*no line items*");
    }

    [Fact]
    public void AddItem_WhenAlreadyConfirmed_ShouldThrowOrderingDomainException()
    {
        // Arrange
        var order = Order.Create("ORD-006", Guid.NewGuid(), CreateSampleAddress());
        order.AddItem(Guid.NewGuid(), "Desk Mat", 20m, 1);
        order.Confirm();

        // Act
        var act = () => order.AddItem(Guid.NewGuid(), "Webcam", 80m, 1);

        // Assert
        act.Should().Throw<OrderingDomainException>()
            .WithMessage("*Cannot add items*");
    }

    [Fact]
    public void Cancel_WhenPending_ShouldTransitionToCanceledAndSetReason()
    {
        // Arrange
        var order = Order.Create("ORD-007", Guid.NewGuid(), CreateSampleAddress());
        order.AddItem(Guid.NewGuid(), "Chair", 250m, 1);
        order.ClearDomainEvents();

        // Act
        order.Cancel("Customer changed mind");

        // Assert
        order.Status.Should().Be(OrderStatus.Canceled);
        order.CancellationReason.Should().Be("Customer changed mind");
        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderCanceledDomainEvent>();
    }

    [Fact]
    public void Cancel_WhenCompleted_ShouldThrowOrderingDomainException()
    {
        // Arrange
        var order = Order.Create("ORD-008", Guid.NewGuid(), CreateSampleAddress());
        order.AddItem(Guid.NewGuid(), "Laptop Stand", 40m, 1);
        order.Confirm();
        order.Complete();

        // Act
        var act = () => order.Cancel("Too late");

        // Assert
        act.Should().Throw<OrderingDomainException>()
            .WithMessage("*already been completed*");
    }
}
