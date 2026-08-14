using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.API.Infrastructure.ErrorHandling;

/// <summary>
/// Centralized <see cref="IExceptionHandler"/> that converts unhandled exceptions into the uniform
/// <see cref="ApiResponse{T}"/> contract with an appropriate status code. Domain exceptions carry
/// safe, controlled messages; all other exceptions return a generic message so internal details are
/// never leaked to clients. Every fault is logged with structured context server-side.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>Creates the handler.</summary>
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        // Client aborted the request: don't treat as a server error and don't attempt to write.
        if (
            exception is OperationCanceledException
            && httpContext.RequestAborted.IsCancellationRequested
        )
        {
            _logger.LogInformation(
                "Request {Path} was cancelled by the client.",
                httpContext.Request.Path
            );
            return true;
        }

        var (statusCode, message) = Map(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}.",
                httpContext.Request.Method,
                httpContext.Request.Path
            );
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Handled {ExceptionType} for {Method} {Path}: {Message}.",
                exception.GetType().Name,
                httpContext.Request.Method,
                httpContext.Request.Path,
                exception.Message
            );
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";
        await httpContext
            .Response.WriteAsJsonAsync(ApiResponse<object>.Failure(message), cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    /// <summary>Maps an exception to an HTTP status and a client-safe message.</summary>
    private static (int StatusCode, string Message) Map(Exception exception) =>
        exception switch
        {
            EntityNotFoundException e => (StatusCodes.Status404NotFound, e.Message),
            DuplicateEntityException e => (StatusCodes.Status409Conflict, e.Message),
            ConcurrencyConflictException e => (StatusCodes.Status409Conflict, e.Message),
            UnauthorizedOperationException e => (StatusCodes.Status403Forbidden, e.Message),
            InsufficientInventoryException e => (
                StatusCodes.Status422UnprocessableEntity,
                e.Message
            ),
            ExpiredInventoryException e => (StatusCodes.Status422UnprocessableEntity, e.Message),
            InvalidOperationDomainException e => (
                StatusCodes.Status422UnprocessableEntity,
                e.Message
            ),
            DomainException e => (StatusCodes.Status400BadRequest, e.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
        };
}
