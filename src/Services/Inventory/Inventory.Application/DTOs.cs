namespace Inventory.Application;

public sealed record InventoryItemDto(
    Guid Id,
    Guid ProductId,
    int TotalQuantity,
    int ReservedQuantity,
    int AvailableQuantity);

public sealed record ReserveStockItemRequest(
    Guid ProductId,
    int Quantity);

public sealed record ReserveStockResponseDto(
    bool IsSuccess,
    string? ErrorMessage);
