using Catalog.Domain;
using FlowCart.BuildingBlocks.Application;
using FlowCart.BuildingBlocks.Domain;

namespace Catalog.Application;

public sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var categoryExists = await _categoryRepository.ExistsAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            return Result.Failure<ProductDto>(Error.NotFound("Category.NotFound", $"Category with id '{request.CategoryId}' was not found."));
        }

        var sku = new Sku(request.Sku);
        var existingSkuProduct = await _productRepository.GetBySkuAsync(sku, cancellationToken);
        if (existingSkuProduct != null)
        {
            return Result.Failure<ProductDto>(Error.Conflict("Product.DuplicateSku", $"A product with SKU '{request.Sku}' already exists."));
        }

        var price = new Money(request.Price, request.Currency);
        var product = Product.Create(request.Name, request.Description, sku, price, request.CategoryId);

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Sku.Value,
            product.Price.Amount,
            product.Price.Currency,
            product.CategoryId,
            product.IsActive);

        return Result.Success(dto);
    }
}

public sealed class UpdateProductPriceCommandHandler : ICommandHandler<UpdateProductPriceCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICatalogCacheService _cacheService;

    public UpdateProductPriceCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ICatalogCacheService cacheService)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<Result<ProductDto>> Handle(UpdateProductPriceCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
        {
            return Result.Failure<ProductDto>(Error.NotFound("Product.NotFound", $"Product with id '{request.ProductId}' was not found."));
        }

        var newPrice = new Money(request.Price, request.Currency);
        product.UpdatePrice(newPrice);

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _cacheService.RemoveAsync($"products:{product.Id}", cancellationToken);

        var dto = new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Sku.Value,
            product.Price.Amount,
            product.Price.Currency,
            product.CategoryId,
            product.IsActive);

        return Result.Success(dto);
    }
}

public sealed class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICatalogCacheService _cacheService;

    public DeleteProductCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ICatalogCacheService cacheService)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
        {
            return Result.Failure(Error.NotFound("Product.NotFound", $"Product with id '{request.ProductId}' was not found."));
        }

        product.Deactivate();
        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync($"products:{product.Id}", cancellationToken);

        return Result.Success();
    }
}

public sealed class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly ICatalogCacheService _cacheService;

    public GetProductByIdQueryHandler(
        IProductRepository productRepository,
        ICatalogCacheService cacheService)
    {
        _productRepository = productRepository;
        _cacheService = cacheService;
    }

    public async Task<Result<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"products:{request.Id}";

        // Cache-Aside Pattern: Read from Redis first
        var cached = await _cacheService.GetAsync<ProductDto>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return Result.Success(cached);
        }

        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product == null)
        {
            return Result.Failure<ProductDto>(Error.NotFound("Product.NotFound", $"Product with id '{request.Id}' was not found."));
        }

        var dto = new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Sku.Value,
            product.Price.Amount,
            product.Price.Currency,
            product.CategoryId,
            product.IsActive);

        // Store into cache with a 10-minute sliding expiration
        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10), cancellationToken);

        return Result.Success(dto);
    }
}

public sealed class GetProductsPagedQueryHandler : IQueryHandler<GetProductsPagedQuery, PagedResult<ProductDto>>
{
    private readonly IProductRepository _productRepository;

    public GetProductsPagedQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<PagedResult<ProductDto>>> Handle(GetProductsPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _productRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            predicate: p => p.IsActive &&
                            (!request.CategoryId.HasValue || p.CategoryId == request.CategoryId.Value) &&
                            (string.IsNullOrEmpty(request.SearchTerm) || p.Name.Contains(request.SearchTerm)),
            orderBy: q => q.OrderBy(p => p.Name),
            cancellationToken: cancellationToken);

        var dtos = items.Select(p => new ProductDto(
            p.Id,
            p.Name,
            p.Description,
            p.Sku.Value,
            p.Price.Amount,
            p.Price.Currency,
            p.CategoryId,
            p.IsActive)).ToList();

        return Result.Success(PagedResult<ProductDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount));
    }
}

public sealed class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategoryCommandHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = Category.Create(request.Name, request.Description);
        await _categoryRepository.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CategoryDto(category.Id, category.Name, category.Description));
    }
}

public sealed class GetCategoriesQueryHandler : IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoriesQueryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        var dtos = categories.Select(c => new CategoryDto(c.Id, c.Name, c.Description)).ToList();
        return Result.Success<IReadOnlyList<CategoryDto>>(dtos);
    }
}

public sealed class ValidateProductPricesCommandHandler : ICommandHandler<ValidateProductPricesCommand, IReadOnlyList<ValidateProductPriceDto>>
{
    private readonly IProductRepository _productRepository;

    public ValidateProductPricesCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<IReadOnlyList<ValidateProductPriceDto>>> Handle(
        ValidateProductPricesCommand request,
        CancellationToken cancellationToken)
    {
        var ids = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _productRepository.GetByIdsAsync(ids, cancellationToken);
        var productMap = products.ToDictionary(p => p.Id);

        var result = new List<ValidateProductPriceDto>();
        foreach (var item in request.Items)
        {
            if (productMap.TryGetValue(item.ProductId, out var product))
            {
                var isMatched = product.Price.Amount == item.ExpectedPrice;
                result.Add(new ValidateProductPriceDto(
                    product.Id,
                    product.Price.Amount,
                    product.Price.Currency,
                    isMatched,
                    product.IsActive));
            }
            else
            {
                result.Add(new ValidateProductPriceDto(
                    item.ProductId,
                    0m,
                    "USD",
                    false,
                    false));
            }
        }

        return Result.Success<IReadOnlyList<ValidateProductPriceDto>>(result);
    }
}
