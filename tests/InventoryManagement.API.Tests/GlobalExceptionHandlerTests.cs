using System.Text.Json;
using InventoryManagement.API.Infrastructure.ErrorHandling;
using InventoryManagement.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace InventoryManagement.API.Tests;

/// <summary>
/// Verifies the status-code contract of <see cref="GlobalExceptionHandler"/> for the duplicate-key
/// race: a uniqueness conflict raised by the database must reach the client as 409, not 500.
/// </summary>
public class GlobalExceptionHandlerTests
{
    private static DefaultHttpContext CreateHttpContext() =>
        new()
        {
            RequestServices = new ServiceCollection().BuildServiceProvider(),
            Response = { Body = new MemoryStream() },
        };

    private static async Task<(int StatusCode, string Body)> HandleAsync(Exception exception)
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var httpContext = CreateHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Path = "/api/inventories";

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);
        Assert.True(handled);

        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body);
        return (httpContext.Response.StatusCode, await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task DuplicateEntityException_FromStoreConstraint_MapsTo409()
    {
        var (statusCode, body) = await HandleAsync(
            new DuplicateEntityException(
                "Inventory",
                new InvalidOperationException("UNIQUE failed")
            )
        );

        Assert.Equal(StatusCodes.Status409Conflict, statusCode);
        using var document = JsonDocument.Parse(body);
        Assert.False(document.RootElement.GetProperty("isSuccess").GetBoolean());
        Assert.Contains(
            "already exists",
            document.RootElement.GetProperty("message").GetString(),
            StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task UnexpectedException_StillMapsTo500_WithoutLeakingDetails()
    {
        var (statusCode, body) = await HandleAsync(new InvalidOperationException("secret detail"));

        Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
        Assert.DoesNotContain("secret detail", body, StringComparison.Ordinal);
    }
}
