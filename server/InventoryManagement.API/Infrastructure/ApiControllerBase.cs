using System.Linq;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Infrastructure;

/// <summary>
/// Base controller that implements the Result → HTTP mapping for the Result design pattern. Services
/// return a <see cref="Result{T}"/>; controllers translate it into an <see cref="ActionResult"/> with
/// the correct status code while preserving the uniform <see cref="ApiResponse{T}"/> response body.
/// </summary>
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Maps a successful result to <c>200 OK</c> and a failure to its mapped status code.</summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="result">The service result.</param>
    protected ActionResult<ApiResponse<T>> HandleResult<T>(Result<T> result) =>
        result.IsSuccess
            ? Ok(ApiResponse<T>.Success(result.Value!, result.Message))
            : ToErrorResult<T>(result);

    /// <summary>
    /// Maps a successful result via a custom success factory (e.g. <c>201 Created</c>), and a failure
    /// to its mapped status code.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="result">The service result.</param>
    /// <param name="onSuccess">Factory producing the success response from the value and message.</param>
    protected ActionResult<ApiResponse<T>> HandleResult<T>(
        Result<T> result,
        Func<T, string, ActionResult<ApiResponse<T>>> onSuccess
    ) => result.IsSuccess ? onSuccess(result.Value!, result.Message) : ToErrorResult<T>(result);

    /// <summary>Converts a failed result into an error <see cref="ApiResponse{T}"/> with its mapped status.</summary>
    private ActionResult<ApiResponse<T>> ToErrorResult<T>(Result result)
    {
        var body = ApiResponse<T>.Failure(result.Message, result.Errors.ToList());
        return StatusCode(MapStatusCode(result.ErrorType), body);
    }

    /// <summary>Maps a <see cref="ResultErrorType"/> to its HTTP status code.</summary>
    private static int MapStatusCode(ResultErrorType errorType) =>
        errorType switch
        {
            ResultErrorType.NotFound => StatusCodes.Status404NotFound,
            ResultErrorType.Conflict => StatusCodes.Status409Conflict,
            ResultErrorType.Validation => StatusCodes.Status400BadRequest,
            ResultErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ResultErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest,
        };
}
