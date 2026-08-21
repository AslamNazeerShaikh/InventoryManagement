using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Mapping;

/// <summary>
/// Hand-written, allocation-light mapping between entities and DTOs. Navigation access is null-safe
/// so records whose related entity was soft-deleted (and filtered out) never trigger a
/// <see cref="NullReferenceException"/> during projection.
/// </summary>
public static class MappingExtensions
{
    /// <summary>Projects a <see cref="User"/> to a <see cref="UserDto"/>.</summary>
    public static UserDto ToDto(this User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            IsAdmin = user.IsAdmin,
            IsProvider = user.IsProvider,
            Role = user.Role,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
        };
    }

    /// <summary>Creates a new <see cref="User"/> from a <see cref="CreateUserDto"/> (password set separately).</summary>
    public static User ToEntity(this CreateUserDto createUserDto)
    {
        return new User
        {
            Name = createUserDto.Name,
            Email = createUserDto.Email,
            IsAdmin = createUserDto.IsAdmin,
            IsProvider = createUserDto.IsProvider,
            Role = createUserDto.Role,
            IsActive = true,
        };
    }

    /// <summary>Applies editable fields from an <see cref="UpdateUserDto"/> onto an existing user.</summary>
    public static void UpdateEntity(this UpdateUserDto updateUserDto, User user)
    {
        user.Name = updateUserDto.Name;
        user.Email = updateUserDto.Email;
        user.IsAdmin = updateUserDto.IsAdmin;
        user.IsProvider = updateUserDto.IsProvider;
        user.Role = updateUserDto.Role;
        user.IsActive = updateUserDto.IsActive;
    }

    /// <summary>Projects an <see cref="Inventory"/> to an <see cref="InventoryDto"/>.</summary>
    public static InventoryDto ToDto(this Inventory inventory)
    {
        return new InventoryDto
        {
            Id = inventory.Id,
            EquipmentName = inventory.EquipmentName,
            Description = inventory.Description,
            Category = inventory.Category,
            Brand = inventory.Brand,
            Model = inventory.Model,
            SerialNumber = inventory.SerialNumber,
            Barcode = inventory.Barcode,
            ExpiryDate = inventory.ExpiryDate,
            ManufactureDate = inventory.ManufactureDate,
            PurchasePrice = inventory.PurchasePrice,
            Supplier = inventory.Supplier,
            Quantity = inventory.Quantity,
            AvailableQuantity = inventory.AvailableQuantity,
            Status = inventory.Status,
            Location = inventory.Location,
            Notes = inventory.Notes,
            CreatedAt = inventory.CreatedAt,
            CreatedByUserName = inventory.CreatedByUser?.Name,
            IsExpiryAlertSent = inventory.IsExpiryAlertSent,
            ReorderLevel = inventory.ReorderLevel,
            ReorderQuantity = inventory.ReorderQuantity,
            SupplierId = inventory.SupplierId,
            SupplierName = inventory.SupplierEntity?.Name,
            LocationId = inventory.LocationId,
            LocationName = inventory.LocationEntity?.Name,
            NeedsReorder =
                inventory.ReorderLevel.HasValue
                && inventory.AvailableQuantity <= inventory.ReorderLevel.Value,
        };
    }

    /// <summary>Creates a new <see cref="Inventory"/> from a <see cref="CreateInventoryDto"/>.</summary>
    public static Inventory ToEntity(this CreateInventoryDto createInventoryDto)
    {
        return new Inventory
        {
            EquipmentName = createInventoryDto.EquipmentName,
            Description = createInventoryDto.Description,
            Category = createInventoryDto.Category,
            Brand = createInventoryDto.Brand,
            Model = createInventoryDto.Model,
            SerialNumber = createInventoryDto.SerialNumber,
            Barcode = createInventoryDto.Barcode,
            ExpiryDate = createInventoryDto.ExpiryDate,
            ManufactureDate = createInventoryDto.ManufactureDate,
            PurchasePrice = createInventoryDto.PurchasePrice,
            Supplier = createInventoryDto.Supplier,
            Quantity = createInventoryDto.Quantity,
            AvailableQuantity = createInventoryDto.Quantity,
            Location = createInventoryDto.Location,
            Notes = createInventoryDto.Notes,
            ReorderLevel = createInventoryDto.ReorderLevel,
            ReorderQuantity = createInventoryDto.ReorderQuantity,
            SupplierId = createInventoryDto.SupplierId,
            LocationId = createInventoryDto.LocationId,
        };
    }

    /// <summary>Applies editable fields from an <see cref="UpdateInventoryDto"/> onto an existing item.</summary>
    public static void UpdateEntity(this UpdateInventoryDto updateInventoryDto, Inventory inventory)
    {
        inventory.EquipmentName = updateInventoryDto.EquipmentName;
        inventory.Description = updateInventoryDto.Description;
        inventory.Category = updateInventoryDto.Category;
        inventory.Brand = updateInventoryDto.Brand;
        inventory.Model = updateInventoryDto.Model;
        inventory.SerialNumber = updateInventoryDto.SerialNumber;
        inventory.Barcode = updateInventoryDto.Barcode;
        inventory.ExpiryDate = updateInventoryDto.ExpiryDate;
        inventory.ManufactureDate = updateInventoryDto.ManufactureDate;
        inventory.PurchasePrice = updateInventoryDto.PurchasePrice;
        inventory.Supplier = updateInventoryDto.Supplier;
        inventory.Quantity = updateInventoryDto.Quantity;
        inventory.Status = updateInventoryDto.Status;
        inventory.Location = updateInventoryDto.Location;
        inventory.Notes = updateInventoryDto.Notes;
        inventory.ReorderLevel = updateInventoryDto.ReorderLevel;
        inventory.ReorderQuantity = updateInventoryDto.ReorderQuantity;
        inventory.SupplierId = updateInventoryDto.SupplierId;
        inventory.LocationId = updateInventoryDto.LocationId;
    }

    /// <summary>Projects an <see cref="InventoryAssignment"/> to an <see cref="InventoryAssignmentDto"/> (null-safe).</summary>
    public static InventoryAssignmentDto ToDto(this InventoryAssignment assignment)
    {
        return new InventoryAssignmentDto
        {
            Id = assignment.Id,
            InventoryId = assignment.InventoryId,
            EquipmentName = assignment.Inventory?.EquipmentName ?? string.Empty,
            Category = assignment.Inventory?.Category,
            Barcode = assignment.Inventory?.Barcode,
            UserId = assignment.UserId,
            UserName = assignment.User?.Name ?? string.Empty,
            UserEmail = assignment.User?.Email ?? string.Empty,
            AssignedQuantity = assignment.AssignedQuantity,
            ReturnedQuantity = assignment.ReturnedQuantity,
            OutstandingQuantity = assignment.AssignedQuantity - assignment.ReturnedQuantity,
            RenewalCount = assignment.RenewalCount,
            ReturnCondition = assignment.ReturnCondition,
            AssignedDate = assignment.AssignedDate,
            ReturnDate = assignment.ReturnDate,
            ExpectedReturnDate = assignment.ExpectedReturnDate,
            Status = assignment.Status,
            AssignmentNotes = assignment.AssignmentNotes,
            ReturnNotes = assignment.ReturnNotes,
            AssignedByUserName = assignment.AssignedByUser?.Name,
            ReturnedToUserName = assignment.ReturnedToUser?.Name,
            CreatedAt = assignment.CreatedAt,
        };
    }

    /// <summary>Creates a new <see cref="InventoryAssignment"/> from a <see cref="CreateInventoryAssignmentDto"/>.</summary>
    public static InventoryAssignment ToEntity(
        this CreateInventoryAssignmentDto createAssignmentDto
    )
    {
        return new InventoryAssignment
        {
            InventoryId = createAssignmentDto.InventoryId,
            UserId = createAssignmentDto.UserId,
            AssignedQuantity = createAssignmentDto.AssignedQuantity,
            ExpectedReturnDate = createAssignmentDto.ExpectedReturnDate,
            AssignmentNotes = createAssignmentDto.AssignmentNotes,
        };
    }

    /// <summary>Applies editable fields from an <see cref="UpdateInventoryAssignmentDto"/> onto an existing assignment.</summary>
    public static void UpdateEntity(
        this UpdateInventoryAssignmentDto updateAssignmentDto,
        InventoryAssignment assignment
    )
    {
        assignment.AssignedQuantity = updateAssignmentDto.AssignedQuantity;
        assignment.ExpectedReturnDate = updateAssignmentDto.ExpectedReturnDate;
        assignment.Status = updateAssignmentDto.Status;
        assignment.AssignmentNotes = updateAssignmentDto.AssignmentNotes;
    }

    /// <summary>Projects a sequence of users to DTOs.</summary>
    public static IEnumerable<UserDto> ToDto(this IEnumerable<User> users) => users.Select(ToDto);

    /// <summary>Projects a sequence of inventory items to DTOs.</summary>
    public static IEnumerable<InventoryDto> ToDto(this IEnumerable<Inventory> inventories) =>
        inventories.Select(ToDto);

    /// <summary>Projects a sequence of assignments to DTOs.</summary>
    public static IEnumerable<InventoryAssignmentDto> ToDto(
        this IEnumerable<InventoryAssignment> assignments
    ) => assignments.Select(ToDto);

    /// <summary>Projects a <see cref="StockMovement"/> ledger row to a <see cref="StockMovementDto"/> (null-safe).</summary>
    public static StockMovementDto ToDto(this StockMovement movement)
    {
        return new StockMovementDto
        {
            Id = movement.Id,
            InventoryId = movement.InventoryId,
            EquipmentName = movement.Inventory?.EquipmentName ?? string.Empty,
            MovementType = movement.MovementType,
            QuantityChange = movement.QuantityChange,
            BalanceAfter = movement.BalanceAfter,
            Reason = movement.Reason,
            Notes = movement.Notes,
            UnitCost = movement.UnitCost,
            PerformedByUserName = movement.PerformedByUser?.Name,
            AssignmentId = movement.AssignmentId,
            FromLocationName = movement.FromLocation?.Name,
            ToLocationName = movement.ToLocation?.Name,
            SupplierName = movement.Supplier?.Name,
            CreatedAt = movement.CreatedAt,
        };
    }

    /// <summary>Projects a sequence of stock movements to DTOs.</summary>
    public static IEnumerable<StockMovementDto> ToDto(this IEnumerable<StockMovement> movements) =>
        movements.Select(ToDto);

    /// <summary>Projects a <see cref="Supplier"/> to a <see cref="SupplierDto"/>.</summary>
    /// <param name="supplier">The supplier entity.</param>
    /// <param name="itemCount">Pre-computed count of items sourced from this supplier (0 when unknown).</param>
    public static SupplierDto ToDto(this Supplier supplier, int itemCount = 0)
    {
        return new SupplierDto
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactName = supplier.ContactName,
            Email = supplier.Email,
            Phone = supplier.Phone,
            Address = supplier.Address,
            Website = supplier.Website,
            LeadTimeDays = supplier.LeadTimeDays,
            IsActive = supplier.IsActive,
            Notes = supplier.Notes,
            ItemCount = itemCount,
            CreatedAt = supplier.CreatedAt,
        };
    }

    /// <summary>Creates a new <see cref="Supplier"/> from a <see cref="CreateSupplierDto"/>.</summary>
    public static Supplier ToEntity(this CreateSupplierDto dto)
    {
        return new Supplier
        {
            Name = dto.Name,
            ContactName = dto.ContactName,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            Website = dto.Website,
            LeadTimeDays = dto.LeadTimeDays,
            Notes = dto.Notes,
            IsActive = true,
        };
    }

    /// <summary>Applies editable fields from an <see cref="UpdateSupplierDto"/> onto an existing supplier.</summary>
    public static void UpdateEntity(this UpdateSupplierDto dto, Supplier supplier)
    {
        supplier.Name = dto.Name;
        supplier.ContactName = dto.ContactName;
        supplier.Email = dto.Email;
        supplier.Phone = dto.Phone;
        supplier.Address = dto.Address;
        supplier.Website = dto.Website;
        supplier.LeadTimeDays = dto.LeadTimeDays;
        supplier.IsActive = dto.IsActive;
        supplier.Notes = dto.Notes;
    }

    /// <summary>Projects a <see cref="Location"/> to a <see cref="LocationDto"/> (null-safe).</summary>
    /// <param name="location">The location entity.</param>
    /// <param name="itemCount">Pre-computed count of items currently in this location (0 when unknown).</param>
    public static LocationDto ToDto(this Location location, int itemCount = 0)
    {
        return new LocationDto
        {
            Id = location.Id,
            Name = location.Name,
            Code = location.Code,
            Description = location.Description,
            ParentLocationId = location.ParentLocationId,
            ParentLocationName = location.ParentLocation?.Name,
            IsActive = location.IsActive,
            ItemCount = itemCount,
            CreatedAt = location.CreatedAt,
        };
    }

    /// <summary>Creates a new <see cref="Location"/> from a <see cref="CreateLocationDto"/>.</summary>
    public static Location ToEntity(this CreateLocationDto dto)
    {
        return new Location
        {
            Name = dto.Name,
            Code = dto.Code,
            Description = dto.Description,
            ParentLocationId = dto.ParentLocationId,
            IsActive = true,
        };
    }

    /// <summary>Applies editable fields from an <see cref="UpdateLocationDto"/> onto an existing location.</summary>
    public static void UpdateEntity(this UpdateLocationDto dto, Location location)
    {
        location.Name = dto.Name;
        location.Code = dto.Code;
        location.Description = dto.Description;
        location.ParentLocationId = dto.ParentLocationId;
        location.IsActive = dto.IsActive;
    }

    /// <summary>Projects a <see cref="MaintenanceSchedule"/> to a <see cref="MaintenanceScheduleDto"/> (null-safe).</summary>
    public static MaintenanceScheduleDto ToDto(this MaintenanceSchedule schedule)
    {
        return new MaintenanceScheduleDto
        {
            Id = schedule.Id,
            InventoryId = schedule.InventoryId,
            EquipmentName = schedule.Inventory?.EquipmentName ?? string.Empty,
            MaintenanceType = schedule.MaintenanceType,
            Title = schedule.Title,
            Description = schedule.Description,
            IntervalDays = schedule.IntervalDays,
            LastPerformedAt = schedule.LastPerformedAt,
            NextDueAt = schedule.NextDueAt,
            Status = schedule.Status,
            PerformedByUserName = schedule.PerformedByUser?.Name,
            Notes = schedule.Notes,
            CreatedAt = schedule.CreatedAt,
        };
    }

    /// <summary>Projects a sequence of maintenance schedules to DTOs.</summary>
    public static IEnumerable<MaintenanceScheduleDto> ToDto(
        this IEnumerable<MaintenanceSchedule> schedules
    ) => schedules.Select(ToDto);

    /// <summary>Creates a new <see cref="MaintenanceSchedule"/> from a <see cref="CreateMaintenanceScheduleDto"/>.</summary>
    public static MaintenanceSchedule ToEntity(this CreateMaintenanceScheduleDto dto)
    {
        return new MaintenanceSchedule
        {
            InventoryId = dto.InventoryId,
            MaintenanceType = dto.MaintenanceType,
            Title = dto.Title,
            Description = dto.Description,
            IntervalDays = dto.IntervalDays,
            NextDueAt = dto.NextDueAt,
        };
    }

    /// <summary>Applies editable fields from an <see cref="UpdateMaintenanceScheduleDto"/> onto an existing schedule.</summary>
    public static void UpdateEntity(this UpdateMaintenanceScheduleDto dto, MaintenanceSchedule schedule)
    {
        schedule.MaintenanceType = dto.MaintenanceType;
        schedule.Title = dto.Title;
        schedule.Description = dto.Description;
        schedule.IntervalDays = dto.IntervalDays;
        schedule.NextDueAt = dto.NextDueAt;
        schedule.Status = dto.Status;
        schedule.Notes = dto.Notes;
    }
}
