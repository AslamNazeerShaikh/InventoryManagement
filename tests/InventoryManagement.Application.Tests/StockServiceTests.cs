using InventoryManagement.Application.Services;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Data;
using InventoryManagement.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace InventoryManagement.Application.Tests;

/// <summary>
/// Integration tests for <see cref="StockService"/> over an in-memory SQLite database, focusing on
/// quantity invariants and the append-only movement ledger written by each operation.
/// </summary>
public sealed class StockServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly StockService _service;

    public StockServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        var unitOfWork = new UnitOfWork(
            _context,
            new UserRepository(_context),
            new InventoryRepository(_context),
            new InventoryAssignmentRepository(_context),
            new StockMovementRepository(_context),
            new SupplierRepository(_context),
            new LocationRepository(_context),
            new MaintenanceScheduleRepository(_context)
        );
        _service = new StockService(unitOfWork, NullLogger<StockService>.Instance);
    }

    [Fact]
    public async Task Receive_IncreasesQuantities_AndWritesLedgerRow()
    {
        var item = await SeedItemAsync(quantity: 10, available: 10);

        var result = await _service.ReceiveStockAsync(
            item.Id,
            new ReceiveStockDto { Quantity = 40, UnitCost = 2.5m, Reason = "PO#1" },
            performedByUserId: 1
        );

        Assert.True(result.IsSuccess);
        var stored = await _context.Inventories.AsNoTracking().SingleAsync();
        Assert.Equal(50, stored.Quantity);
        Assert.Equal(50, stored.AvailableQuantity);

        var movement = await _context.StockMovements.AsNoTracking().SingleAsync();
        Assert.Equal(StockMovementType.Received, movement.MovementType);
        Assert.Equal(40, movement.QuantityChange);
        Assert.Equal(50, movement.BalanceAfter);
    }

    [Fact]
    public async Task Adjust_Negative_ReducesAvailable_AndPreservesAssignedDelta()
    {
        // Qty 10, Available 7 => 3 assigned out. A -2 adjust keeps the 3 assigned intact.
        var item = await SeedItemAsync(quantity: 10, available: 7);

        var result = await _service.AdjustStockAsync(
            item.Id,
            new AdjustStockDto { QuantityDelta = -2, Reason = "Breakage" },
            performedByUserId: 1
        );

        Assert.True(result.IsSuccess);
        var stored = await _context.Inventories.AsNoTracking().SingleAsync();
        Assert.Equal(8, stored.Quantity);
        Assert.Equal(5, stored.AvailableQuantity);
        Assert.Equal(3, stored.Quantity - stored.AvailableQuantity);
    }

    [Fact]
    public async Task Adjust_BelowZeroAvailable_IsRejected()
    {
        var item = await SeedItemAsync(quantity: 3, available: 1);

        var result = await _service.AdjustStockAsync(
            item.Id,
            new AdjustStockDto { QuantityDelta = -5, Reason = "Bad count" },
            performedByUserId: 1
        );

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Empty(await _context.StockMovements.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Dispose_MoreThanAvailable_IsRejected()
    {
        var item = await SeedItemAsync(quantity: 5, available: 2);

        var result = await _service.DisposeStockAsync(
            item.Id,
            new DisposeStockDto { Quantity = 3, Reason = "Expired" },
            performedByUserId: 1
        );

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Dispose_All_MarksItemDisposed()
    {
        var item = await SeedItemAsync(quantity: 4, available: 4);

        var result = await _service.DisposeStockAsync(
            item.Id,
            new DisposeStockDto { Quantity = 4, Reason = "Recall" },
            performedByUserId: 1
        );

        Assert.True(result.IsSuccess);
        var stored = await _context.Inventories.AsNoTracking().SingleAsync();
        Assert.Equal(0, stored.Quantity);
        Assert.Equal(InventoryStatus.Disposed, stored.Status);

        var movement = await _context.StockMovements.AsNoTracking().SingleAsync();
        Assert.Equal(StockMovementType.Disposed, movement.MovementType);
        Assert.Equal(-4, movement.QuantityChange);
    }

    [Fact]
    public async Task Transfer_MovesLocation_AndRecordsFromAndTo()
    {
        var item = await SeedItemAsync(quantity: 5, available: 5);
        var from = new Location { Name = "Store" };
        var to = new Location { Name = "Clinic" };
        _context.Locations.AddRange(from, to);
        await _context.SaveChangesAsync();
        item.LocationId = from.Id;
        await _context.SaveChangesAsync();

        var result = await _service.TransferStockAsync(
            item.Id,
            new TransferStockDto { ToLocationId = to.Id, Reason = "Rebalance" },
            performedByUserId: 1
        );

        Assert.True(result.IsSuccess);
        var stored = await _context.Inventories.AsNoTracking().SingleAsync();
        Assert.Equal(to.Id, stored.LocationId);

        var movement = await _context
            .StockMovements.AsNoTracking()
            .SingleAsync(m => m.MovementType == StockMovementType.Transferred);
        Assert.Equal(from.Id, movement.FromLocationId);
        Assert.Equal(to.Id, movement.ToLocationId);
        Assert.Equal(0, movement.QuantityChange);
    }

    [Fact]
    public async Task Transfer_ToSameLocation_IsRejected()
    {
        var item = await SeedItemAsync(quantity: 5, available: 5);
        var loc = new Location { Name = "Store" };
        _context.Locations.Add(loc);
        await _context.SaveChangesAsync();
        item.LocationId = loc.Id;
        await _context.SaveChangesAsync();

        var result = await _service.TransferStockAsync(
            item.Id,
            new TransferStockDto { ToLocationId = loc.Id },
            performedByUserId: 1
        );

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task GetMovements_ReturnsNewestFirst()
    {
        var item = await SeedItemAsync(quantity: 1, available: 1);
        await _service.ReceiveStockAsync(
            item.Id,
            new ReceiveStockDto { Quantity = 5, Reason = "first" },
            1
        );
        await _service.AdjustStockAsync(
            item.Id,
            new AdjustStockDto { QuantityDelta = -1, Reason = "second" },
            1
        );

        var result = await _service.GetMovementsAsync(item.Id);

        Assert.True(result.IsSuccess);
        var list = result.Value!.ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal(StockMovementType.Adjusted, list[0].MovementType); // newest first
        Assert.Equal(StockMovementType.Received, list[1].MovementType);
    }

    [Fact]
    public async Task Receive_MissingItem_ReturnsNotFound()
    {
        var result = await _service.ReceiveStockAsync(
            999,
            new ReceiveStockDto { Quantity = 1 },
            performedByUserId: 1
        );

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
    }

    private async Task<Inventory> SeedItemAsync(int quantity, int available)
    {
        // Seed the acting user (Id 1) referenced by movement.PerformedByUserId.
        _context.Users.Add(new User { Name = "Op", Email = "op@test.com", PasswordHash = "x" });
        var item = new Inventory
        {
            EquipmentName = "Widget",
            Quantity = quantity,
            AvailableQuantity = available,
            Status = InventoryStatus.Available,
        };
        _context.Inventories.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
