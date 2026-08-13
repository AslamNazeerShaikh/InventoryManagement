using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Tests;

/// <summary>Verifies provider-agnostic optimistic concurrency via the rotating concurrency token.</summary>
public class ConcurrencyTests
{
    [Fact]
    public async Task ConcurrentUpdate_ToSameRow_ThrowsDbUpdateConcurrencyException()
    {
        using var fixture = new SqliteInMemoryFixture();

        var inventory = new Inventory
        {
            EquipmentName = "Ventilator",
            Quantity = 5,
            AvailableQuantity = 5,
            Status = InventoryStatus.Available,
        };
        fixture.Context.Inventories.Add(inventory);
        await fixture.Context.SaveChangesAsync();

        // Two independent contexts load the same row (same original concurrency token).
        await using var ctxA = fixture.NewContext();
        await using var ctxB = fixture.NewContext();

        var fromA = await ctxA.Inventories.SingleAsync(x => x.Id == inventory.Id);
        var fromB = await ctxB.Inventories.SingleAsync(x => x.Id == inventory.Id);

        // First writer wins and rotates the token.
        fromA.AvailableQuantity = 4;
        await ctxA.SaveChangesAsync();

        // Second writer's conditional update matches zero rows → concurrency conflict.
        fromB.AvailableQuantity = 3;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => ctxB.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_RotatesConcurrencyToken_OnUpdate()
    {
        using var fixture = new SqliteInMemoryFixture();

        var inventory = new Inventory { EquipmentName = "Pump", Quantity = 1, AvailableQuantity = 1 };
        fixture.Context.Inventories.Add(inventory);
        await fixture.Context.SaveChangesAsync();
        var original = inventory.ConcurrencyToken;

        inventory.Notes = "updated";
        await fixture.Context.SaveChangesAsync();

        Assert.NotEqual(original, inventory.ConcurrencyToken);
    }
}
