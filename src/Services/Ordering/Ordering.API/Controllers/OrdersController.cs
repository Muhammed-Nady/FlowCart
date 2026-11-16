using FlowCart.BuildingBlocks.Application;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ordering.Application;
using Ordering.Domain;

namespace Ordering.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(new ProblemDetails { Status = 404, Title = result.Error.Code, Detail = result.Error.Description }),
                ErrorType.Conflict => Conflict(new ProblemDetails { Status = 409, Title = result.Error.Code, Detail = result.Error.Description }),
                _ => BadRequest(new ProblemDetails { Status = 400, Title = result.Error.Code, Detail = result.Error.Description })
            };
        }

        return CreatedAtAction(nameof(GetOrderById), new { id = result.Value.Id }, result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetOrderByIdQuery(id), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type == ErrorType.NotFound
                ? NotFound(new ProblemDetails { Status = 404, Title = result.Error.Code, Detail = result.Error.Description })
                : BadRequest(new ProblemDetails { Status = 400, Title = result.Error.Code, Detail = result.Error.Description });
        }

        return Ok(result.Value);
    }

    [HttpGet("customer/{customerId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomerOrders(Guid customerId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCustomerOrdersQuery(customerId), cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] OrderStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetOrdersPagedQuery(pageNumber, pageSize, status), cancellationToken);
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/confirm")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmOrder(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ConfirmOrderCommand(id), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(new ProblemDetails { Status = 404, Title = result.Error.Code, Detail = result.Error.Description }),
                ErrorType.Conflict => Conflict(new ProblemDetails { Status = 409, Title = result.Error.Code, Detail = result.Error.Description }),
                _ => BadRequest(new ProblemDetails { Status = 400, Title = result.Error.Code, Detail = result.Error.Description })
            };
        }

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/cancel")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelOrder(
        Guid id,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CancelOrderCommand(id, request.Reason), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(new ProblemDetails { Status = 404, Title = result.Error.Code, Detail = result.Error.Description }),
                ErrorType.Conflict => Conflict(new ProblemDetails { Status = 409, Title = result.Error.Code, Detail = result.Error.Description }),
                _ => BadRequest(new ProblemDetails { Status = 400, Title = result.Error.Code, Detail = result.Error.Description })
            };
        }

        return Ok(result.Value);
    }
}

public sealed record CancelOrderRequest(string Reason);
