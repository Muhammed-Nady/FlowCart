using FlowCart.BuildingBlocks.Application;
using FlowCart.BuildingBlocks.Domain;
using Inventory.Domain;

namespace Inventory.Application;

public sealed class ReserveStockCommandHandler : ICommandHandler<ReserveStockCommand, ReserveStockResponseDto>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IStockReservationRepository _reservationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReserveStockCommandHandler(
        IInventoryRepository inventoryRepository,
        IStockReservationRepository reservationRepository,
        IUnitOfWork unitOfWork)
    {
        _inventoryRepository = inventoryRepository;
        _reservationRepository = reservationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ReserveStockResponseDto>> Handle(
        ReserveStockCommand request,
        CancellationToken cancellationToken)
    {
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var inventoryItems = await _inventoryRepository.GetByProductIdsAsync(productIds, cancellationToken);
        var inventoryMap = inventoryItems.ToDictionary(i => i.ProductId);

        // 1. Validate availability across all items first
        foreach (var itemRequest in request.Items)
        {
            if (!inventoryMap.TryGetValue(itemRequest.ProductId, out var inventoryItem))
            {
                return Result.Failure<ReserveStockResponseDto>(Error.NotFound(
                    "Inventory.ItemNotFound",
                    $"Inventory record for product '{itemRequest.ProductId}' was not found."));
            }

            if (inventoryItem.AvailableQuantity < itemRequest.Quantity)
            {
                return Result.Failure<ReserveStockResponseDto>(Error.Conflict(
                    "Inventory.InsufficientStock",
                    $"Insufficient stock for product '{itemRequest.ProductId}'. Requested: {itemRequest.Quantity}, Available: {inventoryItem.AvailableQuantity}."));
            }
        }

        // 2. Perform reservations atomically
        try
        {
            foreach (var itemRequest in request.Items)
            {
                var inventoryItem = inventoryMap[itemRequest.ProductId];
                inventoryItem.Reserve(itemRequest.Quantity, request.OrderId);
                _inventoryRepository.Update(inventoryItem);

                var reservation = StockReservation.Create(request.OrderId, itemRequest.ProductId, itemRequest.Quantity);
                await _reservationRepository.AddAsync(reservation, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(new ReserveStockResponseDto(true, null));
        }
        catch (ConcurrencyException)
        {
            return Result.Failure<ReserveStockResponseDto>(Error.Conflict(
                "Inventory.ConcurrencyConflict",
                "A concurrency conflict occurred while reserving stock. Please retry the request."));
        }
        catch (InventoryDomainException ex)
        {
            return Result.Failure<ReserveStockResponseDto>(Error.Conflict("Inventory.RuleViolation", ex.Message));
        }
    }
}

public sealed class ReleaseStockCommandHandler : ICommandHandler<ReleaseStockCommand>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IStockReservationRepository _reservationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReleaseStockCommandHandler(
        IInventoryRepository inventoryRepository,
        IStockReservationRepository reservationRepository,
        IUnitOfWork unitOfWork)
    {
        _inventoryRepository = inventoryRepository;
        _reservationRepository = reservationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ReleaseStockCommand request, CancellationToken cancellationToken)
    {
        var activeReservations = await _reservationRepository.GetActiveByOrderIdAsync(request.OrderId, cancellationToken);
        if (activeReservations.Count == 0)
        {
            // Idempotent: already released or no reservations
            return Result.Success();
        }

        var productIds = activeReservations.Select(r => r.ProductId).Distinct().ToList();
        var inventoryItems = await _inventoryRepository.GetByProductIdsAsync(productIds, cancellationToken);
        var inventoryMap = inventoryItems.ToDictionary(i => i.ProductId);

        foreach (var reservation in activeReservations)
        {
            if (inventoryMap.TryGetValue(reservation.ProductId, out var item))
            {
                item.Release(reservation.Quantity, request.OrderId);
                _inventoryRepository.Update(item);
            }

            reservation.MarkReleased();
            _reservationRepository.Update(reservation);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class RestockItemCommandHandler : ICommandHandler<RestockItemCommand, InventoryItemDto>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RestockItemCommandHandler(IInventoryRepository inventoryRepository, IUnitOfWork unitOfWork)
    {
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<InventoryItemDto>> Handle(RestockItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _inventoryRepository.GetByProductIdAsync(request.ProductId, cancellationToken);

        if (item == null)
        {
            item = InventoryItem.Create(request.ProductId, request.Quantity);
            await _inventoryRepository.AddAsync(item, cancellationToken);
        }
        else
        {
            item.Restock(request.Quantity);
            _inventoryRepository.Update(item);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new InventoryItemDto(
            item.Id,
            item.ProductId,
            item.TotalQuantity,
            item.ReservedQuantity,
            item.AvailableQuantity));
    }
}

public sealed class GetStockByProductIdQueryHandler : IQueryHandler<GetStockByProductIdQuery, InventoryItemDto>
{
    private readonly IInventoryRepository _inventoryRepository;

    public GetStockByProductIdQueryHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<Result<InventoryItemDto>> Handle(GetStockByProductIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _inventoryRepository.GetByProductIdAsync(request.ProductId, cancellationToken);

        if (item == null)
        {
            return Result.Failure<InventoryItemDto>(Error.NotFound(
                "Inventory.NotFound",
                $"No inventory record found for product '{request.ProductId}'."));
        }

        return Result.Success(new InventoryItemDto(
            item.Id,
            item.ProductId,
            item.TotalQuantity,
            item.ReservedQuantity,
            item.AvailableQuantity));
    }
}
