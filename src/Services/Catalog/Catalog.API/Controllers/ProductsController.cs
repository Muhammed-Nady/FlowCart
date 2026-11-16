using Catalog.Application;
using FlowCart.BuildingBlocks.Application;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetProductsPagedQuery(pageNumber, pageSize, categoryId, searchTerm),
            cancellationToken);

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetProductByIdQuery(id), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type == ErrorType.NotFound
                ? NotFound(new ProblemDetails { Status = 404, Title = result.Error.Code, Detail = result.Error.Description })
                : BadRequest(new ProblemDetails { Status = 400, Title = result.Error.Code, Detail = result.Error.Description });
        }

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductCommand command,
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

        return CreatedAtAction(nameof(GetProductById), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id:guid}/price")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePrice(
        Guid id,
        [FromBody] UpdateProductPriceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProductPriceCommand(id, request.Price, request.Currency ?? "USD");
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type == ErrorType.NotFound
                ? NotFound(new ProblemDetails { Status = 404, Title = result.Error.Code, Detail = result.Error.Description })
                : BadRequest(new ProblemDetails { Status = 400, Title = result.Error.Code, Detail = result.Error.Description });
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteProductCommand(id), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type == ErrorType.NotFound
                ? NotFound(new ProblemDetails { Status = 404, Title = result.Error.Code, Detail = result.Error.Description })
                : BadRequest(new ProblemDetails { Status = 400, Title = result.Error.Code, Detail = result.Error.Description });
        }

        return NoContent();
    }

    /// <summary>
    /// Internal endpoint invoked by Ordering Service to validate product prices and availability.
    /// </summary>
    [HttpPost("validate-prices")]
    [ProducesResponseType(typeof(IReadOnlyList<ValidateProductPriceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidatePrices(
        [FromBody] ValidateProductPricesCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result.Value);
    }
}

public sealed record UpdateProductPriceRequest(decimal Price, string? Currency);
