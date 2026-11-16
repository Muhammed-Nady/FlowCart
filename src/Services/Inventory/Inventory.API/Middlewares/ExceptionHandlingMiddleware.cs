using System.Net;
using System.Text.Json;
using FlowCart.BuildingBlocks.Application;
using Inventory.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Middlewares;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception in Inventory Service: {Message}", exception.Message);

        var (statusCode, title, detail, extensions) = exception switch
        {
            ValidationException validationEx => (
                (int)HttpStatusCode.BadRequest,
                "Validation Error",
                "One or more validation errors occurred.",
                new Dictionary<string, object?> { ["errors"] = validationEx.Errors }),

            NotFoundException notFoundEx => (
                (int)HttpStatusCode.NotFound,
                "Resource Not Found",
                notFoundEx.Message,
                null),

            ConcurrencyException concurrencyEx => (
                (int)HttpStatusCode.Conflict,
                "Concurrency Conflict",
                concurrencyEx.Message,
                null),

            InventoryDomainException domainEx => (
                (int)HttpStatusCode.BadRequest,
                "Domain Rule Violation",
                domainEx.Message,
                null),

            _ => (
                (int)HttpStatusCode.InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred while processing inventory.",
                null)
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        if (extensions != null)
        {
            foreach (var (key, value) in extensions)
            {
                problemDetails.Extensions[key] = value;
            }
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
    }
}
