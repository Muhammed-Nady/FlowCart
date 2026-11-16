using FluentAssertions;
using Inventory.Domain;
using Xunit;

namespace Inventory.UnitTests;

public class InventoryDomainTests
{
    [Fact]
    public void Create_ShouldInitializeStockCorrectly()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var item = InventoryItem.Create(productId, 100);

        // Assert
        item.Id.Should().NotBeEmpty();
        item.ProductId.Should().Be(productId);
        item.TotalQuantity.Should().Be(100);
        item.ReservedQuantity.Should().Be(0);
        item.AvailableQuantity.Should().Be(100);
    }

    [Fact]
    public void Reserve_WhenAvailable_ShouldIncreaseReservedQuantityAndRaiseEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var item = InventoryItem.Create(Guid.NewGuid(), 50);

        // Act
        item.Reserve(20, orderId);

        // Assert
        item.ReservedQuantity.Should().Be(20);
        item.AvailableQuantity.Should().Be(30);
        item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StockReservedDomainEvent>()
            .Which.Quantity.Should().Be(20);
    }

    [Fact]
    public void Reserve_WhenInsufficientStock_ShouldThrowInventoryDomainException()
    {
        // Arrange
        var item = InventoryItem.Create(Guid.NewGuid(), 10);

        // Act
        var act = () => item.Reserve(15, Guid.NewGuid());

        // Assert
        act.Should().Throw<InventoryDomainException>()
            .WithMessage("*Insufficient stock*");
    }

    [Fact]
    public void Release_ShouldDecreaseReservedQuantityAndRaiseEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var item = InventoryItem.Create(Guid.NewGuid(), 50);
        item.Reserve(20, orderId);
        item.ClearDomainEvents();

        // Act
        item.Release(15, orderId);

        // Assert
        item.ReservedQuantity.Should().Be(5);
        item.AvailableQuantity.Should().Be(45);
        item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StockReleasedDomainEvent>()
            .Which.Quantity.Should().Be(15);
    }

    [Fact]
    public void Restock_ShouldIncreaseTotalAndAvailableQuantity()
    {
        // Arrange
        var item = InventoryItem.Create(Guid.NewGuid(), 25);
        item.ClearDomainEvents();

        // Act
        item.Restock(75);

        // Assert
        item.TotalQuantity.Should().Be(100);
        item.AvailableQuantity.Should().Be(100);
        item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StockReplenishedDomainEvent>()
            .Which.AddedQuantity.Should().Be(75);
    }
}
