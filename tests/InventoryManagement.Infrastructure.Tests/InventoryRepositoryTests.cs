using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Repositories;

namespace InventoryManagement.Infrastructure.Tests;

public class InventoryRepositoryTests
{
    private static Inventory NewInventory(string name, InventoryStatus status, int available) =>
        new()
        {
            EquipmentName = name,
            Status = status,
            Quantity = available,
            AvailableQuantity = available,
        };

    [Fact]
    public async Task AddAsync_ThenSaveChanges_AssignsIdAndIsRetrievable()
    {
        using var fixture = new SqliteInMemoryFixture();
        var repository = new InventoryRepository(fixture.Context);

        var entity = NewInventory("Defibrillator", InventoryStatus.Available, 2);
        await repository.AddAsync(entity);
        await fixture.Context.SaveChangesAsync();

        Assert.True(entity.Id > 0);
        var loaded = await repository.GetByIdAsync(entity.Id);
        Assert.NotNull(loaded);
        Assert.Equal("Defibrillator", loaded!.EquipmentName);
    }

    [Fact]
    public async Task GetAvailableInventoriesAsync_ReturnsOnlyAvailableWithStock()
    {
        using var fixture = new SqliteInMemoryFixture();
        var repository = new InventoryRepository(fixture.Context);

        await repository.AddAsync(NewInventory("Available-1", InventoryStatus.Available, 3));
        await repository.AddAsync(NewInventory("OutOfStock", InventoryStatus.Available, 0));
        await repository.AddAsync(NewInventory("Assigned", InventoryStatus.Assigned, 5));
        await fixture.Context.SaveChangesAsync();

        var result = await repository.GetAvailableInventoriesAsync();

        Assert.Single(result);
        Assert.Equal("Available-1", result.First().EquipmentName);
    }

    [Fact]
    public async Task IsBarcodeExistsAsync_ReflectsPersistedData()
    {
        using var fixture = new SqliteInMemoryFixture();
        var repository = new InventoryRepository(fixture.Context);

        var entity = NewInventory("Scanner", InventoryStatus.Available, 1);
        entity.Barcode = "BC-123";
        await repository.AddAsync(entity);
        await fixture.Context.SaveChangesAsync();

        Assert.True(await repository.IsBarcodeExistsAsync("BC-123"));
        Assert.False(await repository.IsBarcodeExistsAsync("DOES-NOT-EXIST"));
    }

    [Fact]
    public async Task GetLowStockInventoriesAsync_UsesThreshold()
    {
        using var fixture = new SqliteInMemoryFixture();
        var repository = new InventoryRepository(fixture.Context);

        await repository.AddAsync(NewInventory("Low", InventoryStatus.Available, 2));
        await repository.AddAsync(NewInventory("High", InventoryStatus.Available, 20));
        await fixture.Context.SaveChangesAsync();

        var result = await repository.GetLowStockInventoriesAsync(threshold: 5);

        Assert.Single(result);
        Assert.Equal("Low", result.First().EquipmentName);
    }
}
