using System.Linq.Expressions;
using Catalog.Application;
using Catalog.Domain;
using FlowCart.BuildingBlocks.Application;
using FlowCart.BuildingBlocks.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Catalog.UnitTests;

public class CatalogHandlerTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICatalogCacheService> _cacheMock = new();

    [Fact]
    public async Task CreateProduct_WhenCategoryDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        _categoryRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new CreateProductCommandHandler(_productRepoMock.Object, _categoryRepoMock.Object, _uowMock.Object);
        var command = new CreateProductCommand("Laptop", "Desc", "LAP-001", 1200m, "USD", Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("Category.NotFound");
    }

    [Fact]
    public async Task CreateProduct_WhenSkuAlreadyExists_ShouldReturnConflictError()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _categoryRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var existingProduct = Product.Create("Existing", "Desc", new Sku("LAP-001"), new Money(999m, "USD"), categoryId);
        _productRepoMock.Setup(r => r.GetBySkuAsync(It.IsAny<Sku>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        var handler = new CreateProductCommandHandler(_productRepoMock.Object, _categoryRepoMock.Object, _uowMock.Object);
        var command = new CreateProductCommand("Laptop", "Desc", "LAP-001", 1200m, "USD", categoryId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("Product.DuplicateSku");
    }

    [Fact]
    public async Task CreateProduct_WithValidData_ShouldPersistAndReturnDto()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _categoryRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _productRepoMock.Setup(r => r.GetBySkuAsync(It.IsAny<Sku>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var handler = new CreateProductCommandHandler(_productRepoMock.Object, _categoryRepoMock.Object, _uowMock.Object);
        var command = new CreateProductCommand("Laptop", "Desc", "LAP-001", 1200m, "USD", categoryId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Laptop");
        result.Value.Price.Should().Be(1200m);
        _productRepoMock.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProductById_WhenCached_ShouldReturnFromCacheWithoutQueryingRepository()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var cachedDto = new ProductDto(productId, "Cached Laptop", "Desc", "LAP-CACHED", 999m, "USD", Guid.NewGuid(), true);

        _cacheMock.Setup(c => c.GetAsync<ProductDto>($"products:{productId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedDto);

        var handler = new GetProductByIdQueryHandler(_productRepoMock.Object, _cacheMock.Object);

        // Act
        var result = await handler.Handle(new GetProductByIdQuery(productId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(cachedDto);
        _productRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetProductById_WhenCacheMiss_ShouldQueryRepositoryAndPopulateCache()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var product = Product.Create("DB Laptop", "Desc", new Sku("LAP-DB01"), new Money(850m, "USD"), categoryId);

        _cacheMock.Setup(c => c.GetAsync<ProductDto>($"products:{productId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductDto?)null);

        _productRepoMock.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var handler = new GetProductByIdQueryHandler(_productRepoMock.Object, _cacheMock.Object);

        // Act
        var result = await handler.Handle(new GetProductByIdQuery(productId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("DB Laptop");

        _cacheMock.Verify(c => c.SetAsync(
            $"products:{productId}",
            It.Is<ProductDto>(d => d.Name == "DB Laptop"),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProductPrice_ShouldUpdateDbAndInvalidateCache()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("Headphones", "Desc", new Sku("HDP-001"), new Money(150m, "USD"), Guid.NewGuid());

        _productRepoMock.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var handler = new UpdateProductPriceCommandHandler(_productRepoMock.Object, _uowMock.Object, _cacheMock.Object);
        var command = new UpdateProductPriceCommand(productId, 120m, "USD");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Price.Should().Be(120m);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync($"products:{product.Id}", It.IsAny<CancellationToken>()), Times.Once);
    }
}
