namespace Catalog.Application;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Description,
    string Sku,
    decimal Price,
    string Currency,
    Guid CategoryId,
    bool IsActive);

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Description);

public sealed record ProductPriceCheckItem(
    Guid ProductId,
    decimal ExpectedPrice);

public sealed record ValidateProductPriceDto(
    Guid ProductId,
    decimal ActualPrice,
    string Currency,
    bool IsPriceMatched,
    bool IsActive);
