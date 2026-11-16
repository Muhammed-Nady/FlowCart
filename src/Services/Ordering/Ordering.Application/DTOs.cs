using Ordering.Domain;

namespace Ordering.Application;

public sealed record AddressDto(
    string Street,
    string City,
    string State,
    string Country,
    string ZipCode);

public sealed record OrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice);

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    decimal TotalAmount,
    OrderStatus Status,
    string? CancellationReason,
    AddressDto ShippingAddress,
    IReadOnlyList<OrderItemDto> Items);

public sealed record CreateOrderItemRequest(
    Guid ProductId,
    string ProductName,
    decimal ExpectedUnitPrice,
    int Quantity);

public sealed record ProductPriceCheckRequest(
    Guid ProductId,
    decimal ExpectedPrice);

public sealed record ProductPriceValidationResult(
    Guid ProductId,
    decimal ActualPrice,
    string Currency,
    bool IsPriceMatched,
    bool IsActive);

public sealed record StockReservationItem(
    Guid ProductId,
    int Quantity);

public sealed record ReserveStockResult(
    bool IsSuccess,
    string? ErrorMessage);
