using Catalog.Domain;
using FlowCart.BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

public sealed class ProductRepository : Repository<Product, Guid, CatalogDbContext>, IProductRepository
{
    public ProductRepository(CatalogDbContext context) : base(context)
    {
    }

    public async Task<Product?> GetBySkuAsync(Sku sku, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(p => p.Sku == sku, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        return await DbSet.Where(p => idList.Contains(p.Id)).ToListAsync(cancellationToken);
    }
}

public sealed class CategoryRepository : Repository<Category, Guid, CatalogDbContext>, ICategoryRepository
{
    public CategoryRepository(CatalogDbContext context) : base(context)
    {
    }
}
