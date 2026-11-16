using FlowCart.BuildingBlocks.Application;

namespace Inventory.Application;

public sealed record ReserveStockCommand(
    Guid OrderId,
    IReadOnlyList<ReserveStockItemRequest> Items) : ICommand<ReserveStockResponseDto>;

public sealed record ReleaseStockCommand(Guid OrderId) : ICommand;

public sealed record RestockItemCommand(
    Guid ProductId,
    int Quantity) : ICommand<InventoryItemDto>;
