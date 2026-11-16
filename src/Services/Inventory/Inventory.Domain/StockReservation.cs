using FlowCart.BuildingBlocks.Domain;

namespace Inventory.Domain;

public sealed class StockReservation : AggregateRoot<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private StockReservation() { }

    public StockReservation(Guid id, Guid orderId, Guid productId, int quantity) : base(id)
    {
        if (orderId == Guid.Empty) throw new InventoryDomainException("OrderId cannot be empty.");
        if (productId == Guid.Empty) throw new InventoryDomainException("ProductId cannot be empty.");
        if (quantity <= 0) throw new InventoryDomainException("Quantity must be greater than zero.");

        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        Status = ReservationStatus.Active;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static StockReservation Create(Guid orderId, Guid productId, int quantity)
    {
        return new StockReservation(Guid.NewGuid(), orderId, productId, quantity);
    }

    public void MarkReleased()
    {
        Status = ReservationStatus.Released;
    }

    public void MarkCommitted()
    {
        Status = ReservationStatus.Committed;
    }
}
