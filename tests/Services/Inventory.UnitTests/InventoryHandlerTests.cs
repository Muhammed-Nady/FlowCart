using FlowCart.BuildingBlocks.Application;
using FlowCart.BuildingBlocks.Domain;
using FluentAssertions;
using Inventory.Application;
using Inventory.Domain;
using Moq;
using Xunit;

namespace Inventory.UnitTests;

public class InventoryHandlerTests
{
    private readonly Mock<IInventoryRepository> _inventoryRepoMock = new();
    private readonly Mock<IStockReservationRepository> _reservationRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();

    [Fact]
    public async Task ReserveStock_WhenProductNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _inventoryRepoMock.Setup(r => r.GetByProductIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new ReserveStockCommandHandler(_inventoryRepoMock.Object, _reservationRepoMock.Object, _uowMock.Object);
        var command = new ReserveStockCommand(Guid.NewGuid(), [new ReserveStockItemRequest(productId, 5)]);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("Inventory.ItemNotFound");
    }

    [Fact]
    public async Task ReserveStock_WhenInsufficientQuantity_ShouldReturnConflictError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var item = InventoryItem.Create(productId, 3);
        _inventoryRepoMock.Setup(r => r.GetByProductIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var handler = new ReserveStockCommandHandler(_inventoryRepoMock.Object, _reservationRepoMock.Object, _uowMock.Object);
        var command = new ReserveStockCommand(Guid.NewGuid(), [new ReserveStockItemRequest(productId, 10)]);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("Inventory.InsufficientStock");
    }

    [Fact]
    public async Task ReserveStock_WhenSufficientQuantity_ShouldUpdateAndPersistReservation()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var item = InventoryItem.Create(productId, 50);
        _inventoryRepoMock.Setup(r => r.GetByProductIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var handler = new ReserveStockCommandHandler(_inventoryRepoMock.Object, _reservationRepoMock.Object, _uowMock.Object);
        var command = new ReserveStockCommand(orderId, [new ReserveStockItemRequest(productId, 15)]);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsSuccess.Should().BeTrue();
        item.ReservedQuantity.Should().Be(15);
        item.AvailableQuantity.Should().Be(35);

        _inventoryRepoMock.Verify(r => r.Update(item), Times.Once);
        _reservationRepoMock.Verify(r => r.AddAsync(It.IsAny<StockReservation>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReserveStock_WhenConcurrencyConflictOccurs_ShouldReturnConflictError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var item = InventoryItem.Create(productId, 50);
        _inventoryRepoMock.Setup(r => r.GetByProductIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyException("Row version conflict"));

        var handler = new ReserveStockCommandHandler(_inventoryRepoMock.Object, _reservationRepoMock.Object, _uowMock.Object);
        var command = new ReserveStockCommand(Guid.NewGuid(), [new ReserveStockItemRequest(productId, 5)]);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("Inventory.ConcurrencyConflict");
    }

    [Fact]
    public async Task ReleaseStock_ShouldReleaseInventoryAndMarkReservationReleased()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var item = InventoryItem.Create(productId, 100);
        item.Reserve(20, orderId);

        var reservation = StockReservation.Create(orderId, productId, 20);

        _reservationRepoMock.Setup(r => r.GetActiveByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([reservation]);

        _inventoryRepoMock.Setup(r => r.GetByProductIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var handler = new ReleaseStockCommandHandler(_inventoryRepoMock.Object, _reservationRepoMock.Object, _uowMock.Object);

        // Act
        var result = await handler.Handle(new ReleaseStockCommand(orderId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        item.ReservedQuantity.Should().Be(0);
        item.AvailableQuantity.Should().Be(100);
        reservation.Status.Should().Be(ReservationStatus.Released);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
