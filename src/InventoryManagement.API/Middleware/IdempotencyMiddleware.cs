using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.API.Middleware;

public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Check for Idempotency-Key header
        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var keyHeader) || 
            string.IsNullOrWhiteSpace(keyHeader))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = keyHeader.ToString().Trim();
        var method = context.Request.Method;

        // 2. Apply only to state-modifying requests (POST, PUT, DELETE, PATCH)
        if (method != "POST" && method != "PUT" && method != "DELETE" && method != "PATCH")
        {
            await _next(context);
            return;
        }

        // Validate key length/format to prevent abuse
        if (idempotencyKey.Length > 100)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            var badRequestResponse = ApiResponse<object>.Failure("Idempotency key is too long. Max length is 100 characters.");
            await context.Response.WriteAsJsonAsync(badRequestResponse);
            return;
        }

        _logger.LogInformation("Processing idempotent request with key: {IdempotencyKey}", idempotencyKey);

        var dbContext = context.RequestServices.GetRequiredService<AppDbContext>();

        // 3. Query the DB for existing request record
        var existingRequest = await dbContext.IdempotentRequests
            .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey);

        if (existingRequest != null)
        {
            // 3a. Found and in-progress: prevent double submissions by returning 409 Conflict
            if (!existingRequest.IsCompleted)
            {
                _logger.LogWarning("Conflict detected for idempotency key {IdempotencyKey}. Request is already in progress.", idempotencyKey);
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.ContentType = "application/json";
                var conflictResponse = ApiResponse<object>.Failure("A request with this idempotency key is already in progress.");
                await context.Response.WriteAsJsonAsync(conflictResponse);
                return;
            }

            // 3b. Found and completed: verify that the method and path match to avoid key-collisions
            if (existingRequest.RequestMethod != method || existingRequest.RequestPath != context.Request.Path)
            {
                _logger.LogError("Idempotency key collision detected for key {IdempotencyKey}. Saved: {SavedMethod} {SavedPath}, Incoming: {IncomingMethod} {IncomingPath}",
                    idempotencyKey, existingRequest.RequestMethod, existingRequest.RequestPath, method, context.Request.Path);
                
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";
                var collisionResponse = ApiResponse<object>.Failure("Idempotency key collision: This key was previously used for a different request.");
                await context.Response.WriteAsJsonAsync(collisionResponse);
                return;
            }

            // 3c. Replay the saved response
            _logger.LogInformation("Replaying cached response for idempotency key: {IdempotencyKey}", idempotencyKey);
            context.Response.StatusCode = existingRequest.ResponseStatusCode;
            context.Response.ContentType = existingRequest.ResponseContentType ?? "application/json";
            
            var bodyBytes = Encoding.UTF8.GetBytes(existingRequest.ResponseBody ?? string.Empty);
            await context.Response.Body.WriteAsync(bodyBytes, 0, bodyBytes.Length);
            return;
        }

        // 4. Lock the key by creating a new in-progress record in the database
        var requestRecord = new IdempotentRequest
        {
            IdempotencyKey = idempotencyKey,
            RequestMethod = method,
            RequestPath = context.Request.Path,
            CreatedAt = DateTime.UtcNow,
            IsCompleted = false
        };

        try
        {
            await dbContext.IdempotentRequests.AddAsync(requestRecord);
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Handle concurrent inserts on the database level
            _logger.LogWarning(ex, "DbUpdateException when locking key {IdempotencyKey}. Conflict assumed.", idempotencyKey);
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/json";
            var conflictResponse = ApiResponse<object>.Failure("A request with this idempotency key is already in progress.");
            await context.Response.WriteAsJsonAsync(conflictResponse);
            return;
        }

        // 5. Intercept the response stream
        var originalResponseBodyStream = context.Response.Body;
        using var responseBodyMemoryStream = new MemoryStream();
        context.Response.Body = responseBodyMemoryStream;

        try
        {
            await _next(context);

            // 6. Capture the completed response
            responseBodyMemoryStream.Position = 0;
            string responseBodyText = await new StreamReader(responseBodyMemoryStream).ReadToEndAsync();

            var statusCode = context.Response.StatusCode;

            // Delete the idempotency key for 5xx Server Errors to allow retry, otherwise save it
            if (statusCode >= 500)
            {
                _logger.LogWarning("Request resulted in server error ({StatusCode}). Deleting idempotency key {IdempotencyKey} to allow retry.", statusCode, idempotencyKey);
                dbContext.IdempotentRequests.Remove(requestRecord);
                await dbContext.SaveChangesAsync();
            }
            else
            {
                requestRecord.ResponseStatusCode = statusCode;
                requestRecord.ResponseContentType = context.Response.ContentType;
                requestRecord.ResponseBody = responseBodyText;
                requestRecord.IsCompleted = true;

                dbContext.IdempotentRequests.Update(requestRecord);
                await dbContext.SaveChangesAsync();
            }

            // Copy captured response body back to the original response stream
            responseBodyMemoryStream.Position = 0;
            await responseBodyMemoryStream.CopyToAsync(originalResponseBodyStream);
        }
        catch (Exception ex)
        {
            // On unhandled exception during request execution, delete lock record to allow retries
            _logger.LogError(ex, "Unhandled exception during request with key {IdempotencyKey}. Releasing key lock.", idempotencyKey);
            try
            {
                var cleanupContext = context.RequestServices.GetRequiredService<AppDbContext>();
                var recordToClean = await cleanupContext.IdempotentRequests
                    .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey);
                if (recordToClean != null)
                {
                    cleanupContext.IdempotentRequests.Remove(recordToClean);
                    await cleanupContext.SaveChangesAsync();
                }
            }
            catch (Exception cleanupEx)
            {
                _logger.LogError(cleanupEx, "Failed to clean up idempotency key {IdempotencyKey} after failure.", idempotencyKey);
            }

            throw; // Re-throw the original exception to let standard error handler catch it
        }
        finally
        {
            context.Response.Body = originalResponseBodyStream;
        }
    }
}
