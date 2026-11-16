using FlowCart.BuildingBlocks.Application;
using FlowCart.BuildingBlocks.Domain;
using FluentAssertions;
using Moq;
using Ordering.Application;
using Ordering.Domain;
using Xunit;

namespace Ordering.UnitTests;

public class OrderHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICatalogServiceClient> _catalogMock = new();
    private readonly Mock<IInventoryServiceClient> _inventoryMock = new();

    private static AddressDto CreateSampleAddressDto() =>
        new("123 Enterprise Blvd", "Seattle", "WA", "USA", "98101");

    [Fact]
    public async Task CreateOrder_WhenPriceMismatch_ShouldFailWithoutReservingStock()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _catalogMock.Setup(c => c.ValidatePricesAsync(It.IsAny<IEnumerable<ProductPriceCheckRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ProductPriceValidationResult(productId, 150m, "USD", IsPriceMatched: false, IsActive: true)]);

        var handler = new CreateOrderCommandHandler(_orderRepoMock.Object, _uowMock.Object, _catalogMock.Object, _inventoryMock.Object);
        var command = new CreateOrderCommand(Guid.NewGuid(), CreateSampleAddressDto(), [
            new CreateOrderItemRequest(productId, "Product A", 120m, 1)
        ]);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.PriceMismatch");
        _inventoryMock.Verify(i => i.ReserveStockAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<StockReservationItem>>(), It.IsAny<CancellationToken>()), Times.Never);
        _orderRepoMock.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_WhenProductInactive_ShouldFailWithoutReservingStock()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _catalogMock.Setup(c => c.ValidatePricesAsync(It.IsAny<IEnumerable<ProductPriceCheckRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ProductPriceValidationResult(productId, 120m, "USD", IsPriceMatched: true, IsActive: false)]);

        var handler = new CreateOrderCommandHandler(_orderRepoMock.Object, _uowMock.Object, _catalogMock.Object, _inventoryMock.Object);
        var command = new CreateOrderCommand(Guid.NewGuid(), CreateSampleAddressDto(), [
            new CreateOrderItemRequest(productId, "Product A", 120m, 1)
        ]);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.ProductInactive");
        _inventoryMock.Verify(i => i.ReserveStockAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<StockReservationItem>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_WhenInsufficientStock_ShouldFailWithoutPersistingOrder()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _catalogMock.Setup(c => c.ValidatePricesAsync(It.IsAny<IEnumerable<ProductPriceCheckRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ProductPriceValidationResult(productId, 120m, "USD", IsPriceMatched: true, IsActive: true)]);

        _inventoryMock.Setup(i => i.ReserveStockAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<StockReservationItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReserveStockResult(false, "Item is out of stock."));

        var handler = new CreateOrderCommandHandler(_orderRepoMock.Object, _uowMock.Object, _catalogMock.Object, _inventoryMock.Object);
        var command = new CreateOrderCommand(Guid.NewGuid(), CreateSampleAddressDto(), [
            new CreateOrderItemRequest(productId, "Product A", 120m, 2)
        ]);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.InsufficientStock");
        _orderRepoMock.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_WhenValidationAndStockSucceed_ShouldPersistOrderAndCommit()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _catalogMock.Setup(c => c.ValidatePricesAsync(It.IsAny<IEnumerable<ProductPriceCheckRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ProductPriceValidationResult(productId, 120m, "USD", IsPriceMatched: true, IsActive: true)]);

        _inventoryMock.Setup(i => i.ReserveStockAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<StockReservationItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReserveStockResult(true, null));

        var handler = new CreateOrderCommandHandler(_orderRepoMock.Object, _uowMock.Object, _catalogMock.Object, _inventoryMock.Object);
        var command = new CreateOrderCommand(Guid.NewGuid(), CreateSampleAddressDto(), [
            new CreateOrderItemRequest(productId, "Product A", 120m, 2)
        ]);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAmount.Should().Be(240m);
        result.Value.Status.Should().Be(OrderStatus.Pending);
        _orderRepoMock.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrder_WhenPersistenceThrows_ShouldTriggerCompensatingStockRelease()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _catalogMock.Setup(c => c.ValidatePricesAsync(It.IsAny<IEnumerable<ProductPriceCheckRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ProductPriceValidationResult(productId, 120m, "USD", IsPriceMatched: true, IsActive: true)]);

        _inventoryMock.Setup(i => i.ReserveStockAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<StockReservationItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReserveStockResult(true, null));

        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB deadlock"));

        var handler = new CreateOrderCommandHandler(_orderRepoMock.Object, _uowMock.Object, _catalogMock.Object, _inventoryMock.Object);
        var command = new CreateOrderCommand(Guid.NewGuid(), CreateSampleAddressDto(), [
            new CreateOrderItemRequest(productId, "Product A", 120m, 1)
        ]);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _inventoryMock.Verify(i => i.ReleaseStockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelOrder_ShouldCancelAndReleaseStock()
    {
        // Arrange
        var order = Order.Create("ORD-CANCEL-1", Guid.NewGuid(), new Address("1", "C", "S", "USA", "12345"));
        order.AddItem(Guid.NewGuid(), "Item", 50m, 1);

        _orderRepoMock.Setup(r => r.GetWithItemsByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var handler = new CancelOrderCommandHandler(_orderRepoMock.Object, _uowMock.Object, _inventoryMock.Object);

        // Act
        var result = await handler.Handle(new CancelOrderCommand(order.Id, "Customer request"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(OrderStatus.Canceled);
        _inventoryMock.Verify(i => i.ReleaseStockAsync(order.Id, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
