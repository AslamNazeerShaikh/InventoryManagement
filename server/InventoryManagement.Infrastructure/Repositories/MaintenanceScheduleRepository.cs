using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories;

/// <summary>EF Core repository for <see cref="MaintenanceSchedule"/>.</summary>
public class MaintenanceScheduleRepository
    : GenericRepository<MaintenanceSchedule>,
        IMaintenanceScheduleRepository
{
    /// <summary>Creates the repository.</summary>
    public MaintenanceScheduleRepository(AppDbContext appDbContext)
        : base(appDbContext) { }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MaintenanceSchedule>> GetDueAsync(
        DateTime dueBefore,
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AsNoTracking()
            .Where(m =>
                m.NextDueAt <= dueBefore
                && m.Status != MaintenanceStatus.Completed
                && m.Status != MaintenanceStatus.Cancelled
            )
            .Include(m => m.Inventory)
            .OrderBy(m => m.NextDueAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<MaintenanceSchedule>> GetByInventoryIdAsync(
        int inventoryId,
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AsNoTracking()
            .Where(m => m.InventoryId == inventoryId)
            .Include(m => m.Inventory)
            .Include(m => m.PerformedByUser)
            .OrderBy(m => m.NextDueAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
