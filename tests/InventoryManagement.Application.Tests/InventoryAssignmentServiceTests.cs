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
/// Integration tests for <see cref="InventoryAssignmentService"/> over an in-memory SQLite database,
/// focusing on the stock invariant and the status-transition guard on update.
/// </summary>
public sealed class InventoryAssignmentServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly InventoryAssignmentService _service;

    public InventoryAssignmentServiceTests()
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
        _service = new InventoryAssignmentService(
            unitOfWork,
            NullLogger<InventoryAssignmentService>.Instance
        );
    }

    [Fact]
    public async Task UpdateAssignment_ToReturnedStatus_IsRejected_AndStockUnchanged()
    {
        var (assignment, _) = await SeedActiveAssignmentAsync();

        var result = await _service.UpdateAssignmentAsync(
            assignment.Id,
            new UpdateInventoryAssignmentDto
            {
                AssignedQuantity = assignment.AssignedQuantity,
                Status = AssignmentStatus.Returned, // terminal transition must be rejected here
            }
        );

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);

        // Stock and status must be untouched (no silent leak).
        var inventory = await _context.Inventories.AsNoTracking().SingleAsync();
        var stored = await _context.InventoryAssignments.AsNoTracking().SingleAsync();
        Assert.Equal(3, inventory.AvailableQuantity);
        Assert.Equal(AssignmentStatus.Active, stored.Status);
    }

    [Fact]
    public async Task ReturnAssignment_RestoresStock_WithinTotalQuantity()
    {
        var (assignment, _) = await SeedActiveAssignmentAsync();

        var result = await _service.ReturnAssignmentAsync(
            new ReturnInventoryAssignmentDto { AssignmentId = assignment.Id },
            returnedToUserId: 1
        );

        Assert.True(result.IsSuccess);
        var inventory = await _context.Inventories.AsNoTracking().SingleAsync();
        var stored = await _context.InventoryAssignments.AsNoTracking().SingleAsync();
        Assert.Equal(5, inventory.AvailableQuantity); // 3 available + 2 returned, capped at Quantity 5
        Assert.Equal(AssignmentStatus.Returned, stored.Status);
    }

    [Fact]
    public async Task UpdateAssignment_Missing_ReturnsNotFound()
    {
        var result = await _service.UpdateAssignmentAsync(
            999,
            new UpdateInventoryAssignmentDto { AssignedQuantity = 1, Status = AssignmentStatus.Active }
        );

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task PartialReturn_KeepsAssignmentActive_AndCreditsPartialStock()
    {
        // Item Qty 5 / Available 3, one active assignment of 2.
        var (assignment, _) = await SeedActiveAssignmentAsync();

        var result = await _service.ReturnAssignmentAsync(
            new ReturnInventoryAssignmentDto
            {
                AssignmentId = assignment.Id,
                ReturnQuantity = 1,
                ReturnCondition = ReturnCondition.Good,
            },
            returnedToUserId: 1
        );

        Assert.True(result.IsSuccess);
        var stored = await _context.InventoryAssignments.AsNoTracking().SingleAsync();
        Assert.Equal(1, stored.ReturnedQuantity);
        Assert.Equal(AssignmentStatus.Active, stored.Status); // 1 of 2 still out
        var inventory = await _context.Inventories.AsNoTracking().SingleAsync();
        Assert.Equal(4, inventory.AvailableQuantity); // 3 + 1 returned

        var movement = await _context.StockMovements.AsNoTracking().SingleAsync();
        Assert.Equal(StockMovementType.Returned, movement.MovementType);
        Assert.Equal(1, movement.QuantityChange);
    }

    [Fact]
    public async Task ReturnLost_ReducesOwnedQuantity_InsteadOfCrediting()
    {
        var (assignment, _) = await SeedActiveAssignmentAsync(); // Qty 5, Available 3, assigned 2

        var result = await _service.ReturnAssignmentAsync(
            new ReturnInventoryAssignmentDto
            {
                AssignmentId = assignment.Id,
                ReturnCondition = ReturnCondition.Lost,
            },
            returnedToUserId: 1
        );

        Assert.True(result.IsSuccess);
        var inventory = await _context.Inventories.AsNoTracking().SingleAsync();
        Assert.Equal(3, inventory.Quantity); // 5 - 2 lost
        Assert.Equal(3, inventory.AvailableQuantity); // unchanged (never came back)
        var stored = await _context.InventoryAssignments.AsNoTracking().SingleAsync();
        Assert.Equal(AssignmentStatus.Lost, stored.Status);
    }

    [Fact]
    public async Task Renew_ExtendsExpectedReturn_AndIncrementsCount()
    {
        var (assignment, _) = await SeedActiveAssignmentAsync();

        var result = await _service.RenewAssignmentAsync(
            new RenewInventoryAssignmentDto
            {
                AssignmentId = assignment.Id,
                NewExpectedReturnDate = DateTime.UtcNow.AddDays(30),
            }
        );

        Assert.True(result.IsSuccess);
        var stored = await _context.InventoryAssignments.AsNoTracking().SingleAsync();
        Assert.Equal(1, stored.RenewalCount);
        Assert.NotNull(stored.ExpectedReturnDate);
    }

    /// <summary>Seeds a user, an inventory item (Qty 5, Available 3) and one active assignment (Qty 2).</summary>
    private async Task<(InventoryAssignment Assignment, Inventory Inventory)> SeedActiveAssignmentAsync()
    {
        var user = new User { Name = "Op", Email = "op@test.com", PasswordHash = "x" };
        var inventory = new Inventory
        {
            EquipmentName = "Monitor",
            Quantity = 5,
            AvailableQuantity = 3,
            Status = InventoryStatus.Available,
        };
        _context.Users.Add(user);
        _context.Inventories.Add(inventory);
        await _context.SaveChangesAsync();

        var assignment = new InventoryAssignment
        {
            InventoryId = inventory.Id,
            UserId = user.Id,
            AssignedQuantity = 2,
            Status = AssignmentStatus.Active,
        };
        _context.InventoryAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        return (assignment, inventory);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
