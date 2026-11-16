using FlowCart.BuildingBlocks.Domain;

namespace Ordering.Domain;

public sealed class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = [];

    public string OrderNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public Address ShippingAddress { get; private set; } = default!;
    public OrderStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public decimal TotalAmount => _items.Sum(i => i.TotalPrice);

    private Order() { }

    public Order(Guid id, string orderNumber, Guid customerId, Address shippingAddress) : base(id)
    {
        if (string.IsNullOrWhiteSpace(orderNumber)) throw new OrderingDomainException("OrderNumber cannot be empty.");
        if (customerId == Guid.Empty) throw new OrderingDomainException("CustomerId cannot be empty.");

        OrderNumber = orderNumber.Trim();
        CustomerId = customerId;
        ShippingAddress = shippingAddress ?? throw new ArgumentNullException(nameof(shippingAddress));
        Status = OrderStatus.Pending;

        AddDomainEvent(new OrderCreatedDomainEvent(Id, OrderNumber, CustomerId, TotalAmount));
    }

    public static Order Create(string orderNumber, Guid customerId, Address shippingAddress)
    {
        return new Order(Guid.NewGuid(), orderNumber, customerId, shippingAddress);
    }

    public void AddItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (Status != OrderStatus.Pending)
        {
            throw new OrderingDomainException($"Cannot add items to order in '{Status}' state. Items can only be added to Pending orders.");
        }

        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            var updatedQuantity = existingItem.Quantity + quantity;
            _items.Remove(existingItem);
            _items.Add(new OrderItem(existingItem.Id, Id, productId, productName, unitPrice, updatedQuantity));
        }
        else
        {
            _items.Add(new OrderItem(Guid.NewGuid(), Id, productId, productName, unitPrice, quantity));
        }
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new OrderingDomainException($"Cannot confirm order in '{Status}' state. Only Pending orders can be confirmed.");
        }

        if (_items.Count == 0)
        {
            throw new OrderingDomainException("Cannot confirm an order with no line items.");
        }

        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderConfirmedDomainEvent(Id, OrderNumber));
    }

    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Completed)
        {
            throw new OrderingDomainException("Cannot cancel an order that has already been completed.");
        }

        if (Status == OrderStatus.Canceled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new OrderingDomainException("A cancellation reason is required.");
        }

        Status = OrderStatus.Canceled;
        CancellationReason = reason.Trim();
        AddDomainEvent(new OrderCanceledDomainEvent(Id, OrderNumber, CancellationReason));
    }

    public void Complete()
    {
        if (Status != OrderStatus.Confirmed)
        {
            throw new OrderingDomainException($"Cannot complete order in '{Status}' state. Only Confirmed orders can be marked completed.");
        }

        Status = OrderStatus.Completed;
    }
}
