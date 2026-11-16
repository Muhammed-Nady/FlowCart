using FlowCart.BuildingBlocks.Application;
using FluentAssertions;
using Inventory.API.Controllers;
using Inventory.Application;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Inventory.UnitTests;

public class InventoryControllerTests
{
    private readonly Mock<ISender> _senderMock = new();

    [Fact]
    public async Task ReserveStock_WhenSuccessful_ShouldReturn200Ok()
    {
        // Arrange
        _senderMock.Setup(s => s.Send(It.IsAny<ReserveStockCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new ReserveStockResponseDto(true, null)));

        var controller = new InventoryController(_senderMock.Object);
        var command = new ReserveStockCommand(Guid.NewGuid(), [new ReserveStockItemRequest(Guid.NewGuid(), 2)]);

        // Act
        var result = await controller.ReserveStock(command, CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ReserveStock_WhenConflict_ShouldReturn409Conflict()
    {
        // Arrange
        _senderMock.Setup(s => s.Send(It.IsAny<ReserveStockCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ReserveStockResponseDto>(Error.Conflict("Inventory.InsufficientStock", "Stock unavailable")));

        var controller = new InventoryController(_senderMock.Object);
        var command = new ReserveStockCommand(Guid.NewGuid(), [new ReserveStockItemRequest(Guid.NewGuid(), 2)]);

        // Act
        var result = await controller.ReserveStock(command, CancellationToken.None);

        // Assert
        var conflictResult = result as ConflictObjectResult;
        conflictResult.Should().NotBeNull();
        conflictResult!.StatusCode.Should().Be(409);
    }
}
