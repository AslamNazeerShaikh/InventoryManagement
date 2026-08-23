using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Exceptions;
using InventoryManagement.Infrastructure.Data;
using InventoryManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Tests;

/// <summary>
/// Covers the check-then-act duplicate-detection race (F-12): the store-enforced uniqueness that
/// makes concurrent duplicate creates impossible, and the translation of that store error into a
/// conflict (409) rather than an unhandled fault (500). Uses two contexts over one in-memory SQLite
/// database, which reproduces the race exactly: both writers pass the application-level pre-check
/// before either has committed.
/// </summary>
public class DuplicateDetectionTests
{
    private static UnitOfWork NewUnitOfWork(AppDbContext context) =>
        new(
            context,
            new UserRepository(context),
            new InventoryRepository(context),
            new InventoryAssignmentRepository(context),
            new StockMovementRepository(context),
            new SupplierRepository(context),
            new LocationRepository(context),
            new MaintenanceScheduleRepository(context),
            new RoleRepository(context),
            new PermissionRepository(context),
            new UserRoleRepository(context)
        );

    private static Inventory NewInventory(
        string name,
        string? barcode = null,
        string? serialNumber = null,
        Guid tenantId = default
    ) =>
        new()
        {
            Name = name,
            Status = InventoryStatus.Available,
            Quantity = 1,
            AvailableQuantity = 1,
            Barcode = barcode,
            SerialNumber = serialNumber,
            TenantId = tenantId,
        };

    private static User NewUser(string email) =>
        new()
        {
            Name = "Test User",
            Email = email,
            PasswordHash = "hash",
            IsActive = true,
        };

    [Fact]
    public async Task ConcurrentCreate_WithSameEmail_ThrowsDuplicateEntityException_AndInsertsOneRow()
    {
        using var fixture = new SqliteInMemoryFixture();

        await using var contextA = fixture.NewContext();
        await using var contextB = fixture.NewContext();

        // Both writers pass the pre-check (neither row is committed yet).
        Assert.False(await contextA.Users.AnyAsync(u => u.Email == "race@example.com"));
        Assert.False(await contextB.Users.AnyAsync(u => u.Email == "race@example.com"));

        await contextA.Users.AddAsync(NewUser("race@example.com"));
        await contextB.Users.AddAsync(NewUser("race@example.com"));

        await NewUnitOfWork(contextA).SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<DuplicateEntityException>(() =>
            NewUnitOfWork(contextB).SaveChangesAsync()
        );
        Assert.Contains("User", exception.Message, StringComparison.Ordinal);
        Assert.NotNull(exception.InnerException);

        Assert.Equal(
            1,
            await fixture
                .Context.Users.IgnoreQueryFilters()
                .CountAsync(u => u.Email == "race@example.com")
        );
    }

    [Fact]
    public async Task ConcurrentCreate_WithSameBarcode_ThrowsDuplicateEntityException_AndInsertsOneRow()
    {
        using var fixture = new SqliteInMemoryFixture();

        await using var contextA = fixture.NewContext();
        await using var contextB = fixture.NewContext();

        Assert.False(await contextA.Inventories.AnyAsync(i => i.Barcode == "BC-RACE"));
        Assert.False(await contextB.Inventories.AnyAsync(i => i.Barcode == "BC-RACE"));

        await contextA.Inventories.AddAsync(NewInventory("Scanner A", barcode: "BC-RACE"));
        await contextB.Inventories.AddAsync(NewInventory("Scanner B", barcode: "BC-RACE"));

        await NewUnitOfWork(contextA).SaveChangesAsync();

        await Assert.ThrowsAsync<DuplicateEntityException>(() =>
            NewUnitOfWork(contextB).SaveChangesAsync()
        );

        Assert.Equal(
            1,
            await fixture
                .Context.Inventories.IgnoreQueryFilters()
                .CountAsync(i => i.Barcode == "BC-RACE")
        );
    }

    [Fact]
    public async Task ConcurrentCreate_WithSameSerialNumber_ThrowsDuplicateEntityException_AndInsertsOneRow()
    {
        using var fixture = new SqliteInMemoryFixture();

        await using var contextA = fixture.NewContext();
        await using var contextB = fixture.NewContext();

        await contextA.Inventories.AddAsync(NewInventory("Pump A", serialNumber: "SN-RACE"));
        await contextB.Inventories.AddAsync(NewInventory("Pump B", serialNumber: "SN-RACE"));

        await NewUnitOfWork(contextA).SaveChangesAsync();

        await Assert.ThrowsAsync<DuplicateEntityException>(() =>
            NewUnitOfWork(contextB).SaveChangesAsync()
        );

        Assert.Equal(
            1,
            await fixture
                .Context.Inventories.IgnoreQueryFilters()
                .CountAsync(i => i.SerialNumber == "SN-RACE")
        );
    }

    [Fact]
    public async Task ManyItems_WithoutBarcodeOrSerialNumber_AllPersist()
    {
        using var fixture = new SqliteInMemoryFixture();
        var unitOfWork = NewUnitOfWork(fixture.Context);

        // "Absent" covers both null and empty string: many items legitimately carry neither
        // identifier, so the filtered unique index must not constrain them.
        await fixture.Context.Inventories.AddRangeAsync(
            NewInventory("No identifiers 1"),
            NewInventory("No identifiers 2"),
            NewInventory("Empty identifiers 1", barcode: string.Empty, serialNumber: string.Empty),
            NewInventory("Empty identifiers 2", barcode: string.Empty, serialNumber: string.Empty)
        );

        await unitOfWork.SaveChangesAsync();

        Assert.Equal(4, await fixture.Context.Inventories.CountAsync());
    }

    [Fact]
    public async Task SameBarcodeAndSerialNumber_InDifferentTenants_BothPersist()
    {
        using var fixture = new SqliteInMemoryFixture();
        var unitOfWork = NewUnitOfWork(fixture.Context);

        // Uniqueness is per tenant: two tenants may each own an item with the same barcode.
        await fixture.Context.Inventories.AddRangeAsync(
            NewInventory("Tenant A item", "BC-SHARED", "SN-SHARED", Guid.NewGuid()),
            NewInventory("Tenant B item", "BC-SHARED", "SN-SHARED", Guid.NewGuid())
        );

        await unitOfWork.SaveChangesAsync();

        Assert.Equal(
            2,
            await fixture
                .Context.Inventories.IgnoreQueryFilters()
                .CountAsync(i => i.Barcode == "BC-SHARED")
        );
    }

    [Fact]
    public async Task Barcode_OfSoftDeletedItem_CanBeReused()
    {
        using var fixture = new SqliteInMemoryFixture();
        var unitOfWork = NewUnitOfWork(fixture.Context);

        var original = NewInventory("Retired scanner", barcode: "BC-REUSE");
        await fixture.Context.Inventories.AddAsync(original);
        await unitOfWork.SaveChangesAsync();

        original.IsDeleted = true;
        original.DeletedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync();

        // The soft-delete-aware pre-check reports the barcode as free, so the index must agree.
        await fixture.Context.Inventories.AddAsync(
            NewInventory("New scanner", barcode: "BC-REUSE")
        );
        await unitOfWork.SaveChangesAsync();

        Assert.Equal(1, await fixture.Context.Inventories.CountAsync(i => i.Barcode == "BC-REUSE"));
    }

    [Fact]
    public async Task NonUniqueConstraintViolation_PropagatesAsDbUpdateException()
    {
        using var fixture = new SqliteInMemoryFixture();
        var unitOfWork = NewUnitOfWork(fixture.Context);

        // A NOT NULL violation is not a uniqueness conflict and must keep its original semantics
        // (an unexpected fault) instead of being reported to the caller as a 409.
        var invalid = NewInventory("placeholder");
        invalid.Name = null!;
        await fixture.Context.Inventories.AddAsync(invalid);

        await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.SaveChangesAsync());
    }
}
