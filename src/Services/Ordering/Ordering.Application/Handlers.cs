using FlowCart.BuildingBlocks.Application;
using FlowCart.BuildingBlocks.Domain;
using Ordering.Domain;

namespace Ordering.Application;

public sealed class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICatalogServiceClient _catalogClient;
    private readonly IInventoryServiceClient _inventoryClient;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        ICatalogServiceClient catalogClient,
        IInventoryServiceClient inventoryClient)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _catalogClient = catalogClient;
        _inventoryClient = inventoryClient;
    }

    public async Task<Result<OrderDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. Resilient Price Validation with Catalog Service
        var priceChecks = request.Items.Select(i => new ProductPriceCheckRequest(i.ProductId, i.ExpectedUnitPrice));
        var priceValidationResults = await _catalogClient.ValidatePricesAsync(priceChecks, cancellationToken);

        foreach (var check in priceValidationResults)
        {
            if (!check.IsActive)
            {
                return Result.Failure<OrderDto>(Error.Conflict("Order.ProductInactive",
                    $"Product '{check.ProductId}' is not active or no longer available."));
            }

            if (!check.IsPriceMatched)
            {
                return Result.Failure<OrderDto>(Error.Conflict("Order.PriceMismatch",
                    $"Price for product '{check.ProductId}' has changed. Current price is {check.ActualPrice} {check.Currency}."));
            }
        }

        // 2. Atomic Stock Reservation with Inventory Service
        var orderId = Guid.NewGuid();
        var reservationItems = request.Items.Select(i => new StockReservationItem(i.ProductId, i.Quantity));
        var reservationResult = await _inventoryClient.ReserveStockAsync(orderId, reservationItems, cancellationToken);

        if (!reservationResult.IsSuccess)
        {
            return Result.Failure<OrderDto>(Error.Conflict("Order.InsufficientStock",
                reservationResult.ErrorMessage ?? "One or more items in the order do not have sufficient stock."));
        }

        // 3. Assemble and Persist Order
        try
        {
            var address = new Address(
                request.ShippingAddress.Street,
                request.ShippingAddress.City,
                request.ShippingAddress.State,
                request.ShippingAddress.Country,
                request.ShippingAddress.ZipCode);

            var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            var order = new Order(orderId, orderNumber, request.CustomerId, address);

            foreach (var item in request.Items)
            {
                order.AddItem(item.ProductId, item.ProductName, item.ExpectedUnitPrice, item.Quantity);
            }

            await _orderRepository.AddAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(MapToDto(order));
        }
        catch
        {
            // Compensating transaction: release stock if order persistence fails
            await _inventoryClient.ReleaseStockAsync(orderId, CancellationToken.None);
            throw;
        }
    }

    private static OrderDto MapToDto(Order order)
    {
        var addressDto = new AddressDto(
            order.ShippingAddress.Street,
            order.ShippingAddress.City,
            order.ShippingAddress.State,
            order.ShippingAddress.Country,
            order.ShippingAddress.ZipCode);

        var itemsDto = order.Items.Select(i => new OrderItemDto(
            i.Id,
            i.ProductId,
            i.ProductName,
            i.UnitPrice,
            i.Quantity,
            i.TotalPrice)).ToList();

        return new OrderDto(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            order.TotalAmount,
            order.Status,
            order.CancellationReason,
            addressDto,
            itemsDto);
    }
}

public sealed class ConfirmOrderCommandHandler : ICommandHandler<ConfirmOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmOrderCommandHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderDto>> Handle(ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetWithItemsByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
        {
            return Result.Failure<OrderDto>(Error.NotFound("Order.NotFound", $"Order with id '{request.OrderId}' was not found."));
        }

        try
        {
            order.Confirm();
        }
        catch (OrderingDomainException ex)
        {
            return Result.Failure<OrderDto>(Error.Conflict("Order.InvalidStateTransition", ex.Message));
        }

        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(order));
    }

    private static OrderDto MapToDto(Order order)
    {
        var addressDto = new AddressDto(
            order.ShippingAddress.Street,
            order.ShippingAddress.City,
            order.ShippingAddress.State,
            order.ShippingAddress.Country,
            order.ShippingAddress.ZipCode);

        var itemsDto = order.Items.Select(i => new OrderItemDto(
            i.Id,
            i.ProductId,
            i.ProductName,
            i.UnitPrice,
            i.Quantity,
            i.TotalPrice)).ToList();

        return new OrderDto(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            order.TotalAmount,
            order.Status,
            order.CancellationReason,
            addressDto,
            itemsDto);
    }
}

public sealed class CancelOrderCommandHandler : ICommandHandler<CancelOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryServiceClient _inventoryClient;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IInventoryServiceClient inventoryClient)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _inventoryClient = inventoryClient;
    }

    public async Task<Result<OrderDto>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetWithItemsByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
        {
            return Result.Failure<OrderDto>(Error.NotFound("Order.NotFound", $"Order with id '{request.OrderId}' was not found."));
        }

        try
        {
            order.Cancel(request.Reason);
        }
        catch (OrderingDomainException ex)
        {
            return Result.Failure<OrderDto>(Error.Conflict("Order.InvalidStateTransition", ex.Message));
        }

        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Compensating action: release stock reserved for this order
        await _inventoryClient.ReleaseStockAsync(order.Id, cancellationToken);

        return Result.Success(MapToDto(order));
    }

    private static OrderDto MapToDto(Order order)
    {
        var addressDto = new AddressDto(
            order.ShippingAddress.Street,
            order.ShippingAddress.City,
            order.ShippingAddress.State,
            order.ShippingAddress.Country,
            order.ShippingAddress.ZipCode);

        var itemsDto = order.Items.Select(i => new OrderItemDto(
            i.Id,
            i.ProductId,
            i.ProductName,
            i.UnitPrice,
            i.Quantity,
            i.TotalPrice)).ToList();

        return new OrderDto(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            order.TotalAmount,
            order.Status,
            order.CancellationReason,
            addressDto,
            itemsDto);
    }
}

public sealed class GetOrderByIdQueryHandler : IQueryHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetWithItemsByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
        {
            return Result.Failure<OrderDto>(Error.NotFound("Order.NotFound", $"Order with id '{request.OrderId}' was not found."));
        }

        var addressDto = new AddressDto(
            order.ShippingAddress.Street,
            order.ShippingAddress.City,
            order.ShippingAddress.State,
            order.ShippingAddress.Country,
            order.ShippingAddress.ZipCode);

        var itemsDto = order.Items.Select(i => new OrderItemDto(
            i.Id,
            i.ProductId,
            i.ProductName,
            i.UnitPrice,
            i.Quantity,
            i.TotalPrice)).ToList();

        return Result.Success(new OrderDto(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            order.TotalAmount,
            order.Status,
            order.CancellationReason,
            addressDto,
            itemsDto));
    }
}

public sealed class GetCustomerOrdersQueryHandler : IQueryHandler<GetCustomerOrdersQuery, IReadOnlyList<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetCustomerOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<IReadOnlyList<OrderDto>>> Handle(GetCustomerOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);

        var dtos = orders.Select(order =>
        {
            var addressDto = new AddressDto(
                order.ShippingAddress.Street,
                order.ShippingAddress.City,
                order.ShippingAddress.State,
                order.ShippingAddress.Country,
                order.ShippingAddress.ZipCode);

            var itemsDto = order.Items.Select(i => new OrderItemDto(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.UnitPrice,
                i.Quantity,
                i.TotalPrice)).ToList();

            return new OrderDto(
                order.Id,
                order.OrderNumber,
                order.CustomerId,
                order.TotalAmount,
                order.Status,
                order.CancellationReason,
                addressDto,
                itemsDto);
        }).ToList();

        return Result.Success<IReadOnlyList<OrderDto>>(dtos);
    }
}

public sealed class GetOrdersPagedQueryHandler : IQueryHandler<GetOrdersPagedQuery, PagedResult<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersPagedQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<PagedResult<OrderDto>>> Handle(GetOrdersPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _orderRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            predicate: o => !request.Status.HasValue || o.Status == request.Status.Value,
            orderBy: q => q.OrderByDescending(o => o.OrderNumber),
            cancellationToken: cancellationToken);

        var dtos = items.Select(order =>
        {
            var addressDto = new AddressDto(
                order.ShippingAddress.Street,
                order.ShippingAddress.City,
                order.ShippingAddress.State,
                order.ShippingAddress.Country,
                order.ShippingAddress.ZipCode);

            var itemsDto = order.Items.Select(i => new OrderItemDto(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.UnitPrice,
                i.Quantity,
                i.TotalPrice)).ToList();

            return new OrderDto(
                order.Id,
                order.OrderNumber,
                order.CustomerId,
                order.TotalAmount,
                order.Status,
                order.CancellationReason,
                addressDto,
                itemsDto);
        }).ToList();

        return Result.Success(PagedResult<OrderDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount));
    }
}
