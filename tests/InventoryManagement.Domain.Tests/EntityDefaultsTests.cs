using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Tests;

public class EntityDefaultsTests
{
    [Fact]
    public void User_Defaults_AreSetCorrectly()
    {
        var user = new User();

        Assert.Equal(UserRole.Staff, user.Role);
        Assert.True(user.IsActive);
        Assert.False(user.IsAdmin);
        Assert.False(user.IsProvider);
        Assert.False(user.IsDeleted);
        Assert.NotNull(user.AssignedInventories);
        Assert.NotNull(user.CreatedInventories);
        Assert.Empty(user.AssignedInventories);
    }

    [Fact]
    public void Inventory_Defaults_AreSetCorrectly()
    {
        var inventory = new Inventory();

        Assert.Equal(1, inventory.Quantity);
        Assert.Equal(1, inventory.AvailableQuantity);
        Assert.Equal(InventoryStatus.Available, inventory.Status);
        Assert.False(inventory.IsExpiryAlertSent);
        Assert.NotNull(inventory.Assignments);
    }

    [Fact]
    public void BaseEntity_CreatedAt_DefaultsToRecentUtc()
    {
        var before = DateTime.UtcNow.AddSeconds(-5);

        var inventory = new Inventory();

        Assert.InRange(inventory.CreatedAt, before, DateTime.UtcNow.AddSeconds(5));
        Assert.Null(inventory.UpdatedAt);
        Assert.False(inventory.IsDeleted);
    }
}
