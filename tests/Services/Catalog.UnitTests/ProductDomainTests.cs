using Catalog.Domain;
using FluentAssertions;
using Xunit;

namespace Catalog.UnitTests;

public class ProductDomainTests
{
    [Fact]
    public void CreateProduct_WithValidData_ShouldSucceedAndRaiseDomainEvent()
    {
        // Arrange
        var name = "Gaming Monitor 4K";
        var description = "32 inch 144Hz IPS display";
        var sku = new Sku("MON-4K-32");
        var price = new Money(499.99m, "USD");
        var categoryId = Guid.NewGuid();

        // Act
        var product = Product.Create(name, description, sku, price, categoryId);

        // Assert
        product.Id.Should().NotBeEmpty();
        product.Name.Should().Be(name);
        product.Description.Should().Be(description);
        product.Sku.Should().Be(sku);
        product.Price.Should().Be(price);
        product.CategoryId.Should().Be(categoryId);
        product.IsActive.Should().BeTrue();

        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductCreatedDomainEvent>()
            .Which.Sku.Should().Be("MON-4K-32");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateProduct_WithInvalidName_ShouldThrowCatalogDomainException(string? invalidName)
    {
        // Arrange
        var sku = new Sku("TEST-SKU-01");
        var price = new Money(10m, "USD");

        // Act
        var act = () => Product.Create(invalidName!, "Description", sku, price, Guid.NewGuid());

        // Assert
        act.Should().Throw<CatalogDomainException>();
    }

    [Theory]
    [InlineData("AB")]           // Too short (<3)
    [InlineData("INVALID SKU!")]  // Invalid characters
    [InlineData("")]
    public void Sku_WithInvalidFormat_ShouldThrowCatalogDomainException(string invalidSku)
    {
        // Act
        var act = () => new Sku(invalidSku);

        // Assert
        act.Should().Throw<CatalogDomainException>();
    }

    [Fact]
    public void Money_WithNegativeAmount_ShouldThrowCatalogDomainException()
    {
        // Act
        var act = () => new Money(-5m, "USD");

        // Assert
        act.Should().Throw<CatalogDomainException>();
    }

    [Fact]
    public void UpdatePrice_WithDifferentAmount_ShouldUpdateAndRaisePriceChangedDomainEvent()
    {
        // Arrange
        var product = Product.Create("Mouse", "RGB Mouse", new Sku("MOU-RGB-01"), new Money(50m, "USD"), Guid.NewGuid());
        product.ClearDomainEvents();

        // Act
        product.UpdatePrice(new Money(45m, "USD"));

        // Assert
        product.Price.Amount.Should().Be(45m);
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductPriceChangedDomainEvent>()
            .Which.OldPrice.Should().Be(50m);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var product = Product.Create("Keyboard", "Mechanical", new Sku("KEY-MEC-01"), new Money(100m, "USD"), Guid.NewGuid());

        // Act
        product.Deactivate();

        // Assert
        product.IsActive.Should().BeFalse();
    }
}
