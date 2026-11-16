using FlowCart.BuildingBlocks.Application;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Ordering.API.Controllers;
using Ordering.Application;
using Ordering.Domain;
using Xunit;

namespace Ordering.UnitTests;

public class OrdersControllerTests
{
    private readonly Mock<ISender> _senderMock = new();

    [Fact]
    public async Task CreateOrder_WhenSuccessful_ShouldReturn201Created()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var orderDto = new OrderDto(
            orderId,
            "ORD-20261116-ABCD",
            Guid.NewGuid(),
            120m,
            OrderStatus.Pending,
            null,
            new AddressDto("1", "C", "S", "USA", "12345"),
            []);

        _senderMock.Setup(s => s.Send(It.IsAny<CreateOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(orderDto));

        var controller = new OrdersController(_senderMock.Object);
        var command = new CreateOrderCommand(Guid.NewGuid(), new AddressDto("1", "C", "S", "USA", "12345"), []);

        // Act
        var result = await controller.CreateOrder(command, CancellationToken.None);

        // Assert
        var createdResult = result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.StatusCode.Should().Be(201);
        createdResult.Value.Should().Be(orderDto);
    }

    [Fact]
    public async Task CreateOrder_WhenConflict_ShouldReturn409Conflict()
    {
        // Arrange
        _senderMock.Setup(s => s.Send(It.IsAny<CreateOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<OrderDto>(Error.Conflict("Order.InsufficientStock", "Stock unavailable")));

        var controller = new OrdersController(_senderMock.Object);
        var command = new CreateOrderCommand(Guid.NewGuid(), new AddressDto("1", "C", "S", "USA", "12345"), []);

        // Act
        var result = await controller.CreateOrder(command, CancellationToken.None);

        // Assert
        var conflictResult = result as ConflictObjectResult;
        conflictResult.Should().NotBeNull();
        conflictResult!.StatusCode.Should().Be(409);
    }
}
