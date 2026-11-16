using FlowCart.BuildingBlocks.Domain;

namespace Inventory.Domain;

public sealed class InventoryItem : AggregateRoot<Guid>
{
    public Guid ProductId { get; private set; }
    public int TotalQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int AvailableQuantity => TotalQuantity - ReservedQuantity;
    public byte[] RowVersion { get; private set; } = [];

    private InventoryItem() { }

    public InventoryItem(Guid id, Guid productId, int totalQuantity) : base(id)
    {
        if (productId == Guid.Empty) throw new InventoryDomainException("ProductId cannot be empty.");
        if (totalQuantity < 0) throw new InventoryDomainException("TotalQuantity cannot be negative.");

        ProductId = productId;
        TotalQuantity = totalQuantity;
        ReservedQuantity = 0;
    }

    public static InventoryItem Create(Guid productId, int initialStock)
    {
        return new InventoryItem(Guid.NewGuid(), productId, initialStock);
    }

    public void Reserve(int quantity, Guid orderId)
    {
        if (quantity <= 0)
        {
            throw new InventoryDomainException("Quantity to reserve must be greater than zero.");
        }

        if (quantity > AvailableQuantity)
        {
            throw new InventoryDomainException($"Insufficient stock for product '{ProductId}'. Requested: {quantity}, Available: {AvailableQuantity}.");
        }

        ReservedQuantity += quantity;
        AddDomainEvent(new StockReservedDomainEvent(ProductId, orderId, quantity));
    }

    public void Release(int quantity, Guid orderId)
    {
        if (quantity <= 0)
        {
            throw new InventoryDomainException("Quantity to release must be greater than zero.");
        }

        var amountToRelease = Math.Min(quantity, ReservedQuantity);
        ReservedQuantity -= amountToRelease;

        AddDomainEvent(new StockReleasedDomainEvent(ProductId, orderId, amountToRelease));
    }

    public void Restock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new InventoryDomainException("Restock quantity must be greater than zero.");
        }

        TotalQuantity += quantity;
        AddDomainEvent(new StockReplenishedDomainEvent(ProductId, quantity, TotalQuantity));
    }
}
