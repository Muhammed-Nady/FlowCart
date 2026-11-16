using Catalog.API.Controllers;
using Catalog.Application;
using FlowCart.BuildingBlocks.Application;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Catalog.UnitTests;

public class ProductsControllerTests
{
    private readonly Mock<ISender> _senderMock = new();

    [Fact]
    public async Task GetProductById_WhenFound_ShouldReturn200OkWithProductDto()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var dto = new ProductDto(productId, "Keyboard", "Desc", "KEY-001", 100m, "USD", Guid.NewGuid(), true);
        _senderMock.Setup(s => s.Send(It.IsAny<GetProductByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(dto));

        var controller = new ProductsController(_senderMock.Object);

        // Act
        var actionResult = await controller.GetProductById(productId, CancellationToken.None);

        // Assert
        var okResult = actionResult as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetProductById_WhenNotFound_ShouldReturn404NotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _senderMock.Setup(s => s.Send(It.IsAny<GetProductByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ProductDto>(Error.NotFound("Product.NotFound", "Product not found")));

        var controller = new ProductsController(_senderMock.Object);

        // Act
        var actionResult = await controller.GetProductById(productId, CancellationToken.None);

        // Assert
        var notFoundResult = actionResult as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
    }
}
