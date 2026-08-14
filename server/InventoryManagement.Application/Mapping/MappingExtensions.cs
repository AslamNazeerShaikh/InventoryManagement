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
}
