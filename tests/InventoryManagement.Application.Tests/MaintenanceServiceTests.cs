using InventoryManagement.Application.Services;
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
/// Integration tests for <see cref="MaintenanceService"/>: recurring completion rolls the due date
/// forward, one-off completion closes the schedule, and open statuses are normalized from the due date.
/// </summary>
public sealed class MaintenanceServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly MaintenanceService _service;

    public MaintenanceServiceTests()
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
        _service = new MaintenanceService(unitOfWork, NullLogger<MaintenanceService>.Instance);
    }

    [Fact]
    public async Task Create_SetsDueStatus_WhenWithinWindow()
    {
        var item = await SeedItemAsync();

        var result = await _service.CreateAsync(
            new CreateMaintenanceScheduleDto
            {
                InventoryId = item.Id,
                MaintenanceType = MaintenanceType.Calibration,
                Title = "Annual calibration",
                IntervalDays = 365,
                NextDueAt = DateTime.UtcNow.AddDays(10),
            }
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(MaintenanceStatus.Due, result.Value!.Status); // within the 30-day window
    }

    [Fact]
    public async Task Complete_Recurring_RollsDueDateForward_AndStaysOpen()
    {
        var item = await SeedItemAsync();
        var schedule = new MaintenanceSchedule
        {
            InventoryId = item.Id,
            MaintenanceType = MaintenanceType.Calibration,
            Title = "Annual calibration",
            IntervalDays = 365,
            NextDueAt = DateTime.UtcNow.AddDays(-1),
            Status = MaintenanceStatus.Overdue,
        };
        _context.MaintenanceSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        var performedAt = DateTime.UtcNow;
        var result = await _service.CompleteAsync(
            schedule.Id,
            new CompleteMaintenanceDto { PerformedAt = performedAt, Notes = "OK" },
            performedByUserId: 1
        );

        Assert.True(result.IsSuccess);
        var stored = await _context.MaintenanceSchedules.AsNoTracking().SingleAsync();
        Assert.NotNull(stored.LastPerformedAt);
        Assert.True(stored.NextDueAt > DateTime.UtcNow.AddDays(300)); // rolled ~365 days out
        Assert.NotEqual(MaintenanceStatus.Completed, stored.Status); // recurs, stays open
    }

    [Fact]
    public async Task Complete_OneOff_ClosesSchedule()
    {
        var item = await SeedItemAsync();
        var schedule = new MaintenanceSchedule
        {
            InventoryId = item.Id,
            MaintenanceType = MaintenanceType.Inspection,
            Title = "One-off inspection",
            IntervalDays = null,
            NextDueAt = DateTime.UtcNow.AddDays(2),
            Status = MaintenanceStatus.Due,
        };
        _context.MaintenanceSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        var result = await _service.CompleteAsync(
            schedule.Id,
            new CompleteMaintenanceDto(),
            performedByUserId: 1
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(MaintenanceStatus.Completed, result.Value!.Status);
    }

    private async Task<Inventory> SeedItemAsync()
    {
        // Seed the acting user (Id 1) referenced by schedule.PerformedByUserId on completion.
        _context.Users.Add(new User { Name = "Op", Email = "op@test.com", PasswordHash = "x" });
        var item = new Inventory
        {
            EquipmentName = "BP Monitor",
            Quantity = 1,
            AvailableQuantity = 1,
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
