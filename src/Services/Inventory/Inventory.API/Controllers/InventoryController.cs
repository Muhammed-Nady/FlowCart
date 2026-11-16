using FlowCart.BuildingBlocks.Application;
using Inventory.Application;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly ISender _sender;

    public InventoryController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("reserve")]
    [ProducesResponseType(typeof(ReserveStockResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReserveStock(
        [FromBody] ReserveStockCommand command,
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

        return Ok(result.Value);
    }

    [HttpPost("release")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReleaseStock(
        [FromBody] ReleaseStockCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return Ok(new { Success = result.IsSuccess });
    }

    [HttpGet("products/{productId:guid}")]
    [ProducesResponseType(typeof(InventoryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStockByProductId(Guid productId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetStockByProductIdQuery(productId), cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = result.Error.Code, Detail = result.Error.Description });
        }

        return Ok(result.Value);
    }

    [HttpPost("restock")]
    [ProducesResponseType(typeof(InventoryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Restock(
        [FromBody] RestockItemCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = result.Error.Code, Detail = result.Error.Description });
        }

        return Ok(result.Value);
    }
}
