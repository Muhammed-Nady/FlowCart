using FlowCart.BuildingBlocks.Application;

namespace Catalog.Application;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    string Sku,
    decimal Price,
    string Currency,
    Guid CategoryId) : ICommand<ProductDto>;

public sealed record UpdateProductPriceCommand(
    Guid ProductId,
    decimal Price,
    string Currency = "USD") : ICommand<ProductDto>;

public sealed record DeleteProductCommand(Guid ProductId) : ICommand;

public sealed record CreateCategoryCommand(
    string Name,
    string Description) : ICommand<CategoryDto>;

public sealed record ValidateProductPricesCommand(
    IReadOnlyList<ProductPriceCheckItem> Items) : ICommand<IReadOnlyList<ValidateProductPriceDto>>;
