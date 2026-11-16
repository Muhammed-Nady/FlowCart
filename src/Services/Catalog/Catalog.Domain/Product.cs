using FlowCart.BuildingBlocks.Domain;

namespace Catalog.Domain;

public sealed class Product : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Sku Sku { get; private set; } = default!;
    public Money Price { get; private set; } = default!;
    public Guid CategoryId { get; private set; }
    public bool IsActive { get; private set; }

    private Product() { }

    public Product(Guid id, string name, string description, Sku sku, Money price, Guid categoryId)
        : base(id)
    {
        SetName(name);
        SetDescription(description);
        Sku = sku ?? throw new ArgumentNullException(nameof(sku));
        Price = price ?? throw new ArgumentNullException(nameof(price));
        CategoryId = categoryId != Guid.Empty ? categoryId : throw new CatalogDomainException("Category ID cannot be empty.");
        IsActive = true;

        AddDomainEvent(new ProductCreatedDomainEvent(Id, Name, Sku.Value, Price.Amount, Price.Currency, CategoryId));
    }

    public static Product Create(string name, string description, Sku sku, Money price, Guid categoryId)
    {
        return new Product(Guid.NewGuid(), name, description, sku, price, categoryId);
    }

    public void UpdateDetails(string name, string description, Guid categoryId)
    {
        SetName(name);
        SetDescription(description);

        if (categoryId == Guid.Empty)
        {
            throw new CatalogDomainException("Category ID cannot be empty.");
        }

        CategoryId = categoryId;
    }

    public void UpdatePrice(Money newPrice)
    {
        ArgumentNullException.ThrowIfNull(newPrice);

        if (Price == newPrice)
        {
            return;
        }

        var oldPrice = Price.Amount;
        Price = newPrice;

        AddDomainEvent(new ProductPriceChangedDomainEvent(Id, oldPrice, newPrice.Amount, newPrice.Currency));
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new CatalogDomainException("Product name cannot be empty.");
        }

        Name = name.Trim();
    }

    private void SetDescription(string description)
    {
        Description = description?.Trim() ?? string.Empty;
    }
}
