namespace InventoryManagement.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message)
        : base(message) { }

    public DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}

public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, int id)
        : base($"{entityName} with ID {id} was not found.") { }

    public EntityNotFoundException(string entityName, string identifier)
        : base($"{entityName} with identifier '{identifier}' was not found.") { }
}

public class DuplicateEntityException : DomainException
{
    public DuplicateEntityException(string entityName, string field, string value)
        : base($"{entityName} with {field} '{value}' already exists.") { }
}

public class InvalidOperationDomainException : DomainException
{
    public InvalidOperationDomainException(string message)
        : base(message) { }
}

public class InsufficientInventoryException : DomainException
{
    public InsufficientInventoryException(string equipmentName, int requested, int available)
        : base(
            $"Insufficient inventory for '{equipmentName}'. Requested: {requested}, Available: {available}"
        ) { }
}

public class ExpiredInventoryException : DomainException
{
    public ExpiredInventoryException(string equipmentName, DateTime expiryDate)
        : base(
            $"Cannot assign expired inventory '{equipmentName}'. Expired on: {expiryDate:yyyy-MM-dd}"
        ) { }
}

public class UnauthorizedOperationException : DomainException
{
    public UnauthorizedOperationException(string operation)
        : base($"You are not authorized to perform this operation: {operation}") { }
}
