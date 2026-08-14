namespace InventoryManagement.Domain.Common;

/// <summary>
/// Classifies a failed <see cref="Result"/> so the presentation layer can map it to the correct
/// transport status (e.g. HTTP code) without embedding transport concerns in the application layer.
/// </summary>
public enum ResultErrorType
{
    /// <summary>No error (the result is successful).</summary>
    None = 0,

    /// <summary>A generic business-rule violation.</summary>
    Failure = 1,

    /// <summary>Input failed validation / a precondition was not met.</summary>
    Validation = 2,

    /// <summary>The requested entity does not exist.</summary>
    NotFound = 3,

    /// <summary>The operation conflicts with current state (e.g. duplicate, concurrency).</summary>
    Conflict = 4,

    /// <summary>Authentication failed or credentials were invalid.</summary>
    Unauthorized = 5,

    /// <summary>The caller is authenticated but not permitted to perform the operation.</summary>
    Forbidden = 6,
}

/// <summary>
/// Outcome of an operation using the Result design pattern: an explicit success/failure with a
/// message, an error classification and optional error details — instead of throwing for expected
/// business outcomes. The API layer translates this into an HTTP status code.
/// </summary>
public class Result
{
    /// <summary>Whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Whether the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Human-readable message describing the outcome.</summary>
    public string Message { get; }

    /// <summary>Error classification (<see cref="ResultErrorType.None"/> on success).</summary>
    public ResultErrorType ErrorType { get; }

    /// <summary>Optional detailed error messages (e.g. per-field validation errors).</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>Initializes a result.</summary>
    protected Result(
        bool isSuccess,
        string message,
        ResultErrorType errorType,
        IReadOnlyList<string>? errors
    )
    {
        IsSuccess = isSuccess;
        Message = message;
        ErrorType = errorType;
        Errors = errors ?? Array.Empty<string>();
    }

    /// <summary>Creates a successful result.</summary>
    public static Result Success(string message = "Operation successful") =>
        new(true, message, ResultErrorType.None, null);

    /// <summary>Creates a generic business-rule failure.</summary>
    public static Result Failure(string message, IReadOnlyList<string>? errors = null) =>
        new(false, message, ResultErrorType.Failure, errors);

    /// <summary>Creates a not-found failure.</summary>
    public static Result NotFound(string message) =>
        new(false, message, ResultErrorType.NotFound, null);

    /// <summary>Creates a conflict failure.</summary>
    public static Result Conflict(string message) =>
        new(false, message, ResultErrorType.Conflict, null);

    /// <summary>Creates a validation failure.</summary>
    public static Result Validation(string message, IReadOnlyList<string>? errors = null) =>
        new(false, message, ResultErrorType.Validation, errors);

    /// <summary>Creates an unauthorized failure.</summary>
    public static Result Unauthorized(string message) =>
        new(false, message, ResultErrorType.Unauthorized, null);

    /// <summary>Creates a forbidden failure.</summary>
    public static Result Forbidden(string message) =>
        new(false, message, ResultErrorType.Forbidden, null);
}

/// <summary>A <see cref="Result"/> that carries a value on success.</summary>
/// <typeparam name="T">Type of the value produced on success.</typeparam>
public sealed class Result<T> : Result
{
    /// <summary>The value produced on success; <c>default</c> on failure.</summary>
    public T? Value { get; }

    private Result(
        bool isSuccess,
        T? value,
        string message,
        ResultErrorType errorType,
        IReadOnlyList<string>? errors
    )
        : base(isSuccess, message, errorType, errors)
    {
        Value = value;
    }

    /// <summary>Creates a successful result wrapping <paramref name="value"/>.</summary>
    public static Result<T> Success(T value, string message = "Operation successful") =>
        new(true, value, message, ResultErrorType.None, null);

    /// <summary>Creates a generic business-rule failure.</summary>
    public static new Result<T> Failure(string message, IReadOnlyList<string>? errors = null) =>
        new(false, default, message, ResultErrorType.Failure, errors);

    /// <summary>Creates a not-found failure.</summary>
    public static new Result<T> NotFound(string message) =>
        new(false, default, message, ResultErrorType.NotFound, null);

    /// <summary>Creates a conflict failure.</summary>
    public static new Result<T> Conflict(string message) =>
        new(false, default, message, ResultErrorType.Conflict, null);

    /// <summary>Creates a validation failure.</summary>
    public static new Result<T> Validation(
        string message,
        IReadOnlyList<string>? errors = null
    ) => new(false, default, message, ResultErrorType.Validation, errors);

    /// <summary>Creates an unauthorized failure.</summary>
    public static new Result<T> Unauthorized(string message) =>
        new(false, default, message, ResultErrorType.Unauthorized, null);

    /// <summary>Creates a forbidden failure.</summary>
    public static new Result<T> Forbidden(string message) =>
        new(false, default, message, ResultErrorType.Forbidden, null);
}
