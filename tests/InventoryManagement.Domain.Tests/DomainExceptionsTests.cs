using InventoryManagement.Domain.Exceptions;

namespace InventoryManagement.Domain.Tests;

public class DomainExceptionsTests
{
    [Fact]
    public void EntityNotFoundException_WithId_FormatsMessage()
    {
        var ex = new EntityNotFoundException("Inventory", 42);

        Assert.Equal("Inventory with ID 42 was not found.", ex.Message);
        Assert.IsAssignableFrom<DomainException>(ex);
    }

    [Fact]
    public void DuplicateEntityException_FormatsMessage()
    {
        var ex = new DuplicateEntityException("User", "Email", "a@b.com");

        Assert.Equal("User with Email 'a@b.com' already exists.", ex.Message);
    }

    [Fact]
    public void InsufficientInventoryException_FormatsMessage()
    {
        var ex = new InsufficientInventoryException("Syringe", 10, 3);

        Assert.Contains("Syringe", ex.Message);
        Assert.Contains("Requested: 10", ex.Message);
        Assert.Contains("Available: 3", ex.Message);
    }
}
