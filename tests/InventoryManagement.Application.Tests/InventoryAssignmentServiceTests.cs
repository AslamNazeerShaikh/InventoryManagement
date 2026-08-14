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
            new InventoryAssignmentRepository(_context)
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
