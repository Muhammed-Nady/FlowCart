using FlowCart.BuildingBlocks.Domain;

namespace Ordering.Domain;

public sealed class OrderItem : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal TotalPrice => UnitPrice * Quantity;

    private OrderItem() { }

    public OrderItem(Guid id, Guid orderId, Guid productId, string productName, decimal unitPrice, int quantity)
        : base(id)
    {
        if (orderId == Guid.Empty) throw new OrderingDomainException("OrderId cannot be empty.");
        if (productId == Guid.Empty) throw new OrderingDomainException("ProductId cannot be empty.");
        if (string.IsNullOrWhiteSpace(productName)) throw new OrderingDomainException("ProductName cannot be empty.");
        if (unitPrice <= 0) throw new OrderingDomainException("UnitPrice must be greater than zero.");
        if (quantity <= 0) throw new OrderingDomainException("Quantity must be greater than zero.");

        OrderId = orderId;
        ProductId = productId;
        ProductName = productName.Trim();
        UnitPrice = decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero);
        Quantity = quantity;
    }
}
