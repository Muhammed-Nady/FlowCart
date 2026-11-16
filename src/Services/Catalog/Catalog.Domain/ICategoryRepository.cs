using FlowCart.BuildingBlocks.Domain;

namespace Catalog.Domain;

public interface ICategoryRepository : IRepository<Category, Guid>
{
}
