using FlowCart.BuildingBlocks.Domain;

namespace Catalog.Domain;

public sealed class Category : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private Category() { }

    public Category(Guid id, string name, string description) : base(id)
    {
        SetName(name);
        SetDescription(description);
    }

    public static Category Create(string name, string description)
    {
        return new Category(Guid.NewGuid(), name, description);
    }

    public void Update(string name, string description)
    {
        SetName(name);
        SetDescription(description);
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new CatalogDomainException("Category name cannot be empty.");
        }

        Name = name.Trim();
    }

    private void SetDescription(string description)
    {
        Description = description?.Trim() ?? string.Empty;
    }
}
