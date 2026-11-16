using FlowCart.BuildingBlocks.Application;

namespace Inventory.Application;

public sealed record GetStockByProductIdQuery(Guid ProductId) : IQuery<InventoryItemDto>;
