using FlowCart.BuildingBlocks.Application;

namespace Catalog.Application;

public sealed record GetProductByIdQuery(Guid Id) : IQuery<ProductDto>;

public sealed record GetProductsPagedQuery(
    int PageNumber = 1,
    int PageSize = 10,
    Guid? CategoryId = null,
    string? SearchTerm = null) : IQuery<PagedResult<ProductDto>>;

public sealed record GetCategoriesQuery : IQuery<IReadOnlyList<CategoryDto>>;
