namespace InventoryManagement.Domain.Exceptions;

/// <summary>
/// Base type for all domain-level exceptions. These represent expected business-rule
/// violations (as opposed to infrastructure faults) and are translated to appropriate
/// HTTP status codes by the API's global exception handler.
/// </summary>
public class DomainException : Exception
{
    /// <summary>Creates a domain exception with the specified message.</summary>
    public DomainException(string message)
        : base(message) { }

    /// <summary>Creates a domain exception wrapping an underlying cause.</summary>
    public DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>Raised when a requested entity does not exist. Maps to HTTP 404.</summary>
public class EntityNotFoundException : DomainException
{
    /// <summary>Creates the exception for a numeric identifier.</summary>
    public EntityNotFoundException(string entityName, int id)
        : base($"{entityName} with ID {id} was not found.") { }

    /// <summary>Creates the exception for a string identifier.</summary>
    public EntityNotFoundException(string entityName, string identifier)
        : base($"{entityName} with identifier '{identifier}' was not found.") { }
}

/// <summary>Raised when creating/updating an entity would violate a uniqueness constraint. Maps to HTTP 409.</summary>
public class DuplicateEntityException : DomainException
{
    /// <summary>Creates the exception describing the conflicting field/value.</summary>
    public DuplicateEntityException(string entityName, string field, string value)
        : base($"{entityName} with {field} '{value}' already exists.") { }
}

/// <summary>Raised when an operation is invalid for the current entity state. Maps to HTTP 422.</summary>
public class InvalidOperationDomainException : DomainException
{
    /// <summary>Creates the exception with an explanatory message.</summary>
    public InvalidOperationDomainException(string message)
        : base(message) { }
}

/// <summary>Raised when insufficient inventory is available to satisfy a request. Maps to HTTP 422.</summary>
public class InsufficientInventoryException : DomainException
{
    /// <summary>Creates the exception describing requested versus available quantities.</summary>
    public InsufficientInventoryException(string equipmentName, int requested, int available)
        : base(
            $"Insufficient inventory for '{equipmentName}'. Requested: {requested}, Available: {available}"
        ) { }
}

/// <summary>Raised when attempting to assign expired inventory. Maps to HTTP 422.</summary>
public class ExpiredInventoryException : DomainException
{
    /// <summary>Creates the exception describing the expired item.</summary>
    public ExpiredInventoryException(string equipmentName, DateTime expiryDate)
        : base(
            $"Cannot assign expired inventory '{equipmentName}'. Expired on: {expiryDate:yyyy-MM-dd}"
        ) { }
}

/// <summary>Raised when the caller is not permitted to perform an operation. Maps to HTTP 403.</summary>
public class UnauthorizedOperationException : DomainException
{
    /// <summary>Creates the exception naming the forbidden operation.</summary>
    public UnauthorizedOperationException(string operation)
        : base($"You are not authorized to perform this operation: {operation}") { }
}

/// <summary>
/// Raised when an optimistic-concurrency conflict is detected (a concurrent writer changed the
/// row after it was read). Maps to HTTP 409 so the client can retry with fresh state.
/// </summary>
public class ConcurrencyConflictException : DomainException
{
    /// <summary>Creates the exception describing the entity that could not be saved.</summary>
    public ConcurrencyConflictException(string entityName)
        : base(
            $"The {entityName} was modified by another operation. Please retry with the latest data."
        ) { }
}
