using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories;

public class InventoryAssignmentRepository
    : GenericRepository<InventoryAssignment>,
        IInventoryAssignmentRepository
{
    private readonly AppDbContext _appDbContext;

    public InventoryAssignmentRepository(AppDbContext appDbContext)
        : base(appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<IEnumerable<InventoryAssignment>> GetAssignmentsByUserIdAsync(int userId)
    {
        return await _appDbContext
            .InventoryAssignments.Where(x => x.UserId == userId)
            .Include(x => x.Inventory)
            .Include(x => x.User)
            .Include(x => x.AssignedByUser)
            .OrderByDescending(x => x.AssignedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<InventoryAssignment>> GetAssignmentsByInventoryIdAsync(
        int inventoryId
    )
    {
        return await _appDbContext
            .InventoryAssignments.Where(x => x.InventoryId == inventoryId)
            .Include(x => x.Inventory)
            .Include(x => x.User)
            .Include(x => x.AssignedByUser)
            .OrderByDescending(x => x.AssignedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<InventoryAssignment>> GetActiveAssignmentsAsync()
    {
        return await _appDbContext
            .InventoryAssignments.Where(x => x.Status == AssignmentStatus.Active)
            .Include(x => x.Inventory)
            .Include(x => x.User)
            .Include(x => x.AssignedByUser)
            .OrderByDescending(x => x.AssignedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<InventoryAssignment>> GetActiveAssignmentsByUserIdAsync(
        int userId
    )
    {
        return await _appDbContext
            .InventoryAssignments.Where(x =>
                x.UserId == userId && x.Status == AssignmentStatus.Active
            )
            .Include(x => x.Inventory)
            .Include(x => x.User)
            .Include(x => x.AssignedByUser)
            .OrderByDescending(x => x.AssignedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<InventoryAssignment>> GetAssignmentsByStatusAsync(
        AssignmentStatus status
    )
    {
        return await _appDbContext
            .InventoryAssignments.Where(x => x.Status == status)
            .Include(x => x.Inventory)
            .Include(x => x.User)
            .Include(x => x.AssignedByUser)
            .OrderByDescending(x => x.AssignedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<InventoryAssignment>> GetOverdueAssignmentsAsync()
    {
        var today = DateTime.UtcNow.Date;
        return await _appDbContext
            .InventoryAssignments.Where(x =>
                x.Status == AssignmentStatus.Active
                && x.ExpectedReturnDate.HasValue
                && x.ExpectedReturnDate.Value.Date < today
            )
            .Include(x => x.Inventory)
            .Include(x => x.User)
            .Include(x => x.AssignedByUser)
            .OrderBy(x => x.ExpectedReturnDate)
            .ToListAsync();
    }

    public async Task<InventoryAssignment?> GetActiveAssignmentAsync(int inventoryId, int userId)
    {
        return await _appDbContext.InventoryAssignments.FirstOrDefaultAsync(x =>
            x.InventoryId == inventoryId
            && x.UserId == userId
            && x.Status == AssignmentStatus.Active
        );
    }

    public async Task<bool> HasActiveAssignmentAsync(int inventoryId, int userId)
    {
        return await _appDbContext.InventoryAssignments.AnyAsync(x =>
            x.InventoryId == inventoryId
            && x.UserId == userId
            && x.Status == AssignmentStatus.Active
        );
    }

    public async Task ReturnAssignmentAsync(
        int assignmentId,
        int returnedToUserId,
        string? returnNotes = null
    )
    {
        var assignment = await _appDbContext.InventoryAssignments.FindAsync(assignmentId);
        if (assignment != null)
        {
            assignment.Status = AssignmentStatus.Returned;
            assignment.ReturnDate = DateTime.UtcNow;
            assignment.ReturnedToUserId = returnedToUserId;
            assignment.ReturnNotes = returnNotes;
            _appDbContext.InventoryAssignments.Update(assignment);
        }
    }

    public async Task<IEnumerable<InventoryAssignment>> GetAssignmentHistoryAsync(int inventoryId)
    {
        return await _appDbContext
            .InventoryAssignments.Where(x => x.InventoryId == inventoryId)
            .Include(x => x.User)
            .Include(x => x.AssignedByUser)
            .Include(x => x.ReturnedToUser)
            .OrderByDescending(x => x.AssignedDate)
            .ToListAsync();
    }
}
