using InventoryManagement.Application.Mapping;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Application.Services;

/// <summary>
/// Maintenance/calibration schedule service. Recurring schedules roll their due date forward on
/// completion. Non-terminal statuses are normalized to Scheduled/Due/Overdue at read time from the
/// due date, so the UI stays accurate without a background job.
/// </summary>
public sealed class MaintenanceService : IMaintenanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MaintenanceService> _logger;

    /// <summary>Creates the maintenance service.</summary>
    public MaintenanceService(IUnitOfWork unitOfWork, ILogger<MaintenanceService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<MaintenanceScheduleDto>>> GetByInventoryIdAsync(
        int inventoryId,
        CancellationToken cancellationToken = default
    )
    {
        var exists = await _unitOfWork
            .Inventories.AnyAsync(x => x.Id == inventoryId, cancellationToken)
            .ConfigureAwait(false);
        if (!exists)
        {
            return Result<IEnumerable<MaintenanceScheduleDto>>.NotFound("Inventory not found");
        }

        var schedules = await _unitOfWork
            .MaintenanceSchedules.GetByInventoryIdAsync(inventoryId, cancellationToken)
            .ConfigureAwait(false);
        return Result<IEnumerable<MaintenanceScheduleDto>>.Success(ToDisplayDtos(schedules));
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<MaintenanceScheduleDto>>> GetDueAsync(
        int daysAhead = 30,
        CancellationToken cancellationToken = default
    )
    {
        daysAhead = Math.Clamp(daysAhead, 0, BusinessConstants.Maintenance.MaxDueWindowDays);
        var dueBefore = DateTime.UtcNow.AddDays(daysAhead);
        var schedules = await _unitOfWork
            .MaintenanceSchedules.GetDueAsync(dueBefore, cancellationToken)
            .ConfigureAwait(false);
        return Result<IEnumerable<MaintenanceScheduleDto>>.Success(ToDisplayDtos(schedules));
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<MaintenanceScheduleDto>>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default
    )
    {
        pageSize = Math.Clamp(pageSize, 1, BusinessConstants.Pagination.MaxPageSize);

        var page = await _unitOfWork
            .MaintenanceSchedules.GetPagedAsync(
                pageNumber,
                pageSize,
                orderBy: q => q.OrderBy(x => x.NextDueAt),
                include: q => q.Include(x => x.Inventory).Include(x => x.PerformedByUser),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        var result = new PagedResult<MaintenanceScheduleDto>
        {
            Data = ToDisplayDtos(page.Items),
            TotalCount = page.TotalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
        return Result<PagedResult<MaintenanceScheduleDto>>.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result<MaintenanceScheduleDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var schedule = await LoadForDtoAsync(id, cancellationToken).ConfigureAwait(false);
        return schedule is null
            ? Result<MaintenanceScheduleDto>.NotFound("Maintenance schedule not found")
            : Result<MaintenanceScheduleDto>.Success(ToDisplayDto(schedule));
    }

    /// <inheritdoc />
    public async Task<Result<MaintenanceScheduleDto>> CreateAsync(
        CreateMaintenanceScheduleDto createDto,
        CancellationToken cancellationToken = default
    )
    {
        var inventoryExists = await _unitOfWork
            .Inventories.AnyAsync(x => x.Id == createDto.InventoryId, cancellationToken)
            .ConfigureAwait(false);
        if (!inventoryExists)
        {
            return Result<MaintenanceScheduleDto>.Validation("Inventory not found");
        }

        var schedule = createDto.ToEntity();
        schedule.Status = ComputeDisplayStatus(MaintenanceStatus.Scheduled, schedule.NextDueAt);
        await _unitOfWork
            .MaintenanceSchedules.AddAsync(schedule, cancellationToken)
            .ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created maintenance schedule {ScheduleId}.", schedule.Id);
        var reloaded = await LoadForDtoAsync(schedule.Id, cancellationToken).ConfigureAwait(false);
        return Result<MaintenanceScheduleDto>.Success(
            ToDisplayDto(reloaded!),
            "Maintenance schedule created successfully"
        );
    }

    /// <inheritdoc />
    public async Task<Result<MaintenanceScheduleDto>> UpdateAsync(
        int id,
        UpdateMaintenanceScheduleDto updateDto,
        CancellationToken cancellationToken = default
    )
    {
        var schedule = await _unitOfWork
            .MaintenanceSchedules.GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);
        if (schedule is null)
        {
            return Result<MaintenanceScheduleDto>.NotFound("Maintenance schedule not found");
        }

        updateDto.UpdateEntity(schedule);
        // Keep non-terminal statuses consistent with the (possibly changed) due date.
        schedule.Status = ComputeDisplayStatus(schedule.Status, schedule.NextDueAt);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated maintenance schedule {ScheduleId}.", id);
        var reloaded = await LoadForDtoAsync(id, cancellationToken).ConfigureAwait(false);
        return Result<MaintenanceScheduleDto>.Success(
            ToDisplayDto(reloaded!),
            "Maintenance schedule updated successfully"
        );
    }

    /// <inheritdoc />
    public async Task<Result<MaintenanceScheduleDto>> CompleteAsync(
        int id,
        CompleteMaintenanceDto completeDto,
        int performedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var schedule = await _unitOfWork
            .MaintenanceSchedules.GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);
        if (schedule is null)
        {
            return Result<MaintenanceScheduleDto>.NotFound("Maintenance schedule not found");
        }

        if (schedule.Status == MaintenanceStatus.Cancelled)
        {
            return Result<MaintenanceScheduleDto>.Validation(
                "Cannot complete a cancelled schedule"
            );
        }

        var performedAt = completeDto.PerformedAt ?? DateTime.UtcNow;
        schedule.LastPerformedAt = performedAt;
        schedule.PerformedByUserId = performedByUserId;
        if (!string.IsNullOrWhiteSpace(completeDto.Notes))
        {
            schedule.Notes = completeDto.Notes;
        }

        // Roll forward when a next date is given explicitly or derivable from the interval; otherwise
        // this was a one-off and the schedule closes.
        var nextDue =
            completeDto.NextDueAt
            ?? (
                schedule.IntervalDays.HasValue
                    ? performedAt.AddDays(schedule.IntervalDays.Value)
                    : (DateTime?)null
            );

        if (nextDue.HasValue)
        {
            schedule.NextDueAt = nextDue.Value;
            schedule.Status = ComputeDisplayStatus(MaintenanceStatus.Scheduled, nextDue.Value);
        }
        else
        {
            schedule.Status = MaintenanceStatus.Completed;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Completed maintenance schedule {ScheduleId}.", id);
        var reloaded = await LoadForDtoAsync(id, cancellationToken).ConfigureAwait(false);
        return Result<MaintenanceScheduleDto>.Success(
            ToDisplayDto(reloaded!),
            "Maintenance recorded successfully"
        );
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var schedule = await _unitOfWork
            .MaintenanceSchedules.GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);
        if (schedule is null)
        {
            return Result<bool>.NotFound("Maintenance schedule not found");
        }

        schedule.IsDeleted = true;
        schedule.DeletedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Soft-deleted maintenance schedule {ScheduleId}.", id);
        return Result<bool>.Success(true, "Maintenance schedule deleted successfully");
    }

    /// <summary>Loads a schedule (tracking-free) with item + performer navigations for projection.</summary>
    private Task<MaintenanceSchedule?> LoadForDtoAsync(
        int id,
        CancellationToken cancellationToken
    ) =>
        _unitOfWork.MaintenanceSchedules.FirstOrDefaultAsync(
            x => x.Id == id,
            include: q => q.Include(m => m.Inventory).Include(m => m.PerformedByUser),
            cancellationToken: cancellationToken
        );

    private static IEnumerable<MaintenanceScheduleDto> ToDisplayDtos(
        IEnumerable<MaintenanceSchedule> schedules
    ) => schedules.Select(ToDisplayDto);

    /// <summary>Projects a schedule to a DTO with a freshly-computed non-terminal status.</summary>
    private static MaintenanceScheduleDto ToDisplayDto(MaintenanceSchedule schedule)
    {
        var dto = schedule.ToDto();
        dto.Status = ComputeDisplayStatus(schedule.Status, schedule.NextDueAt);
        return dto;
    }

    /// <summary>
    /// Derives the display status from the due date for open schedules; terminal statuses
    /// (Completed/Cancelled) are preserved.
    /// </summary>
    private static MaintenanceStatus ComputeDisplayStatus(
        MaintenanceStatus stored,
        DateTime nextDueAt
    )
    {
        if (stored is MaintenanceStatus.Completed or MaintenanceStatus.Cancelled)
        {
            return stored;
        }

        var now = DateTime.UtcNow;
        if (nextDueAt < now)
        {
            return MaintenanceStatus.Overdue;
        }

        return nextDueAt <= now.AddDays(BusinessConstants.Maintenance.DefaultDueWindowDays)
            ? MaintenanceStatus.Due
            : MaintenanceStatus.Scheduled;
    }
}
