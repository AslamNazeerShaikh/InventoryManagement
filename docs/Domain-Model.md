# Domain Model Documentation - Inventory Management System

## Overview

This document details the domain model structure, including entities, enums, DTOs, value objects, and their relationships within the Domain layer of the Inventory Management System.

## Domain Architecture

The domain layer follows **Domain-Driven Design (DDD)** principles with:

- **Rich Domain Entities** with business logic
- **Value Objects** for complex types
- **Domain Events** (future consideration)
- **Aggregate Roots** for consistency boundaries
- **Repository Interfaces** for data access abstraction

---

## Core Entities

### 1. User Entity

**Namespace**: `InventoryManagement.Domain.Entities`
**Purpose**: Represents system users with role-based access control

```csharp
public class User : BaseEntity
{
    // Identity Properties
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    // Role and Permissions
    public UserRole Role { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsProvider { get; set; }

    // Authentication
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    // Soft Delete
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual ICollection<InventoryAssignment> Assignments { get; set; } = new List<InventoryAssignment>();
    public virtual ICollection<InventoryAssignment> AssignmentsMade { get; set; } = new List<InventoryAssignment>();
    public virtual ICollection<InventoryAssignment> ReturnsProcessed { get; set; } = new List<InventoryAssignment>();
}
```

**Key Features:**

- **Role-based Security**: Admin, NursePractitioner, Staff roles
- **JWT Authentication**: Refresh token support
- **Audit Trail**: Tracks who made/processed assignments
- **Soft Delete**: Maintains historical integrity

---

### 2. Inventory Entity

**Namespace**: `InventoryManagement.Domain.Entities`
**Purpose**: Represents medical equipment and supplies

```csharp
public class Inventory : BaseEntity
{
    // Basic Information
    public string EquipmentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }

    // Identification
    public string? SerialNumber { get; set; }
    public string? Barcode { get; set; }

    // Quantity and Cost
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }

    // Lifecycle Management
    public DateTime? ExpiryDate { get; set; }
    public InventoryStatus Status { get; set; } = InventoryStatus.Available;

    // Location and Notes
    public string? Location { get; set; }
    public string? Notes { get; set; }

    // Soft Delete
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual ICollection<InventoryAssignment> Assignments { get; set; } = new List<InventoryAssignment>();

    // Business Methods
    public bool IsExpiringSoon(int monthsThreshold = 3) =>
        ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.UtcNow.AddMonths(monthsThreshold);

    public bool IsLowStock(int threshold = 5) =>
        Quantity <= threshold;

    public bool CanAssign(int requestedQuantity) =>
        Status == InventoryStatus.Available &&
        Quantity >= requestedQuantity &&
        (!ExpiryDate.HasValue || ExpiryDate > DateTime.UtcNow);
}
```

**Key Features:**

- **Barcode Support**: For scanning operations
- **Expiry Tracking**: Date-based expiration management
- **Status Management**: Comprehensive status tracking
- **Business Logic**: Built-in validation methods

---

### 3. InventoryAssignment Entity

**Namespace**: `InventoryManagement.Domain.Entities`
**Purpose**: Tracks equipment assignments with complete lifecycle

```csharp
public class InventoryAssignment : BaseEntity
{
    // References
    public int InventoryId { get; set; }
    public int UserId { get; set; }
    public int AssignedByUserId { get; set; }
    public int? ReturnedToUserId { get; set; }

    // Assignment Details
    public int QuantityAssigned { get; set; }
    public DateTime AssignedDate { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Active;
    public string? Notes { get; set; }

    // Return Details
    public int? QuantityReturned { get; set; }
    public DateTime? ActualReturnDate { get; set; }
    public string? ReturnCondition { get; set; }
    public string? ReturnNotes { get; set; }

    // Soft Delete
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual Inventory Inventory { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual User AssignedByUser { get; set; } = null!;
    public virtual User? ReturnedToUser { get; set; }

    // Business Methods
    public bool IsOverdue() =>
        Status == AssignmentStatus.Active &&
        ExpectedReturnDate.HasValue &&
        ExpectedReturnDate < DateTime.UtcNow;

    public bool CanReturn() =>
        Status == AssignmentStatus.Active;

    public void ProcessReturn(int returnedQuantity, string condition, string? notes, int processedByUserId)
    {
        QuantityReturned = returnedQuantity;
        ActualReturnDate = DateTime.UtcNow;
        ReturnCondition = condition;
        ReturnNotes = notes;
        ReturnedToUserId = processedByUserId;
        Status = AssignmentStatus.Returned;
    }
}
```

**Key Features:**

- **Complete Audit Trail**: Tracks who assigned and who processed returns
- **Quantity Tracking**: Supports partial returns
- **Status Lifecycle**: From Active to Returned/Expired/Lost/Damaged
- **Business Logic**: Built-in validation and processing methods

---

## Base Entity

### BaseEntity Abstract Class

**Purpose**: Provides common audit fields for all entities

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
```

**Benefits:**

- **Automatic Auditing**: CreatedAt, UpdatedAt timestamps
- **User Tracking**: CreatedBy, UpdatedBy for accountability
- **Consistent Interface**: All entities inherit common properties

---

## Enums

### 1. UserRole Enum

```csharp
public enum UserRole
{
    Admin = 1,
    NursePractitioner = 2,
    Staff = 3
}
```

**Usage:**

- **Admin**: Full system access, user management
- **NursePractitioner**: Can manage inventory and assignments
- **Staff**: Read-only access to assigned items

### 2. InventoryStatus Enum

```csharp
public enum InventoryStatus
{
    Available = 1,
    Assigned = 2,
    Reserved = 3,
    Expired = 4,
    Damaged = 5,
    Disposed = 6
}
```

**Lifecycle:**

- **Available**: Ready for assignment
- **Assigned**: Currently assigned to users
- **Reserved**: Reserved for specific use
- **Expired**: Past expiration date
- **Damaged**: Needs repair or disposal
- **Disposed**: Permanently removed from inventory

### 3. AssignmentStatus Enum

```csharp
public enum AssignmentStatus
{
    Active = 1,
    Returned = 2,
    Expired = 3,
    Lost = 4,
    Damaged = 5
}
```

**Lifecycle:**

- **Active**: Currently assigned and in use
- **Returned**: Successfully returned
- **Expired**: Assignment period expired
- **Lost**: Equipment reported lost
- **Damaged**: Equipment damaged during assignment

---

## DTOs (Data Transfer Objects)

### 1. User DTOs

#### UserDto

```csharp
public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsProvider { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

#### CreateUserDto

```csharp
public class CreateUserDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }
    public bool IsProvider { get; set; }
}
```

#### UpdateUserDto

```csharp
public class UpdateUserDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }
    public bool IsProvider { get; set; }
}
```

### 2. Authentication DTOs

#### LoginDto

```csharp
public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
```

#### AuthResponseDto

```csharp
public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}
```

#### ChangePasswordDto

```csharp
public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required, Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

### 3. Inventory DTOs

#### InventoryDto

```csharp
public class InventoryDto
{
    public int Id { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Barcode { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public bool IsExpiringSoon { get; set; }
    public bool IsLowStock { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

#### CreateInventoryDto

```csharp
public class CreateInventoryDto
{
    [Required, MaxLength(200)]
    public string EquipmentName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required, MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Manufacturer { get; set; }

    [MaxLength(100)]
    public string? Model { get; set; }

    [MaxLength(100)]
    public string? SerialNumber { get; set; }

    [MaxLength(50)]
    public string? Barcode { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required, Range(0.01, double.MaxValue)]
    public decimal UnitCost { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
```

### 4. Assignment DTOs

#### InventoryAssignmentDto

```csharp
public class InventoryAssignmentDto
{
    public int Id { get; set; }
    public int InventoryId { get; set; }
    public string InventoryName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int AssignedByUserId { get; set; }
    public string AssignedByUserName { get; set; } = string.Empty;
    public int QuantityAssigned { get; set; }
    public DateTime AssignedDate { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int? QuantityReturned { get; set; }
    public DateTime? ActualReturnDate { get; set; }
    public string? ReturnCondition { get; set; }
    public string? ReturnNotes { get; set; }
    public bool IsOverdue { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 5. Dashboard DTOs

#### DashboardStatsDto

```csharp
public class DashboardStatsDto
{
    public int TotalInventoryItems { get; set; }
    public int AvailableItems { get; set; }
    public int AssignedItems { get; set; }
    public int ExpiredItems { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int NursePractitioners { get; set; }
    public int TotalAssignments { get; set; }
    public int ActiveAssignments { get; set; }
    public int OverdueAssignments { get; set; }
    public int ExpiringItemsCount { get; set; }
    public int LowStockItemsCount { get; set; }
    public decimal TotalInventoryValue { get; set; }
}
```

---

## Common DTOs

### 1. ApiResponse<T>

**Purpose**: Standardized API response wrapper

```csharp
public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> Success(T data, string message = "Success")
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message
        };
    }

    public static ApiResponse<T> Failure(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }
}
```

### 2. PagedResult<T>

**Purpose**: Pagination support for large datasets

```csharp
public class PagedResult<T>
{
    public IEnumerable<T> Data { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
```

---

## Domain Constants

### 1. AuthConstants

```csharp
public static class AuthConstants
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string NursePractitioner = "NursePractitioner";
        public const string Staff = "Staff";
    }

    public static class Claims
    {
        public const string UserId = "userId";
        public const string Email = "email";
        public const string Name = "name";
        public const string Role = "role";
        public const string IsAdmin = "isAdmin";
        public const string IsProvider = "isProvider";
    }

    public static class Policies
    {
        public const string AdminOnly = "AdminOnly";
        public const string AdminOrProvider = "AdminOrProvider";
        public const string AllRoles = "AllRoles";
    }
}
```

### 2. BusinessConstants

```csharp
public static class BusinessConstants
{
    public static class ExpiryAlert
    {
        public const int DefaultMonthsBefore = 3;
        public const int MinMonthsBefore = 1;
        public const int MaxMonthsBefore = 12;
    }

    public static class Inventory
    {
        public const int LowStockThreshold = 5;
        public const int MaxQuantity = 10000;
    }

    public static class Pagination
    {
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 100;
    }
}
```

---

## Repository Interfaces

### 1. IGenericRepository<T>

```csharp
public interface IGenericRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize);
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
```

### 2. Specialized Repository Interfaces

```csharp
public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetByRoleAsync(UserRole role);
    Task<IEnumerable<User>> GetActiveUsersAsync();
    Task<bool> EmailExistsAsync(string email);
}

public interface IInventoryRepository : IGenericRepository<Inventory>
{
    Task<Inventory?> GetByBarcodeAsync(string barcode);
    Task<IEnumerable<Inventory>> GetByCategoryAsync(string category);
    Task<IEnumerable<Inventory>> GetExpiringAsync(int monthsBefore);
    Task<IEnumerable<Inventory>> GetLowStockAsync(int threshold);
    Task<IEnumerable<Inventory>> SearchAsync(InventorySearchDto searchDto);
}

public interface IInventoryAssignmentRepository : IGenericRepository<InventoryAssignment>
{
    Task<IEnumerable<InventoryAssignment>> GetByUserIdAsync(int userId);
    Task<IEnumerable<InventoryAssignment>> GetByInventoryIdAsync(int inventoryId);
    Task<IEnumerable<InventoryAssignment>> GetActiveAssignmentsAsync();
    Task<IEnumerable<InventoryAssignment>> GetOverdueAssignmentsAsync();
    Task<AssignmentHistoryDto> GetAssignmentHistoryAsync(int inventoryId);
}
```

---

## Domain Relationships Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                             DOMAIN MODEL OVERVIEW                          │
└─────────────────────────────────────────────────────────────────────────────┘

                                  BaseEntity
                                      ↑
                     ┌────────────────┼────────────────┐
                     │                │                │
                   User         Inventory    InventoryAssignment
                     │                │                │
                     │                │                │
              ┌──────┴──────┐        │         ┌──────┴──────┐
              │             │        │         │             │
           UserDto    CreateUserDto   │   AssignmentDto CreateAssignmentDto
         UpdateUserDto   LoginDto     │    ReturnDto    UpdateAssignmentDto
        AuthResponseDto              │
                                     │
                              ┌─────┴─────┐
                              │           │
                        InventoryDto  CreateInventoryDto
                       SearchDto     UpdateInventoryDto

┌─────────────────────────────────────────────────────────────────────────────┐
│                              RELATIONSHIP FLOWS                            │
└─────────────────────────────────────────────────────────────────────────────┘

User ──(1:M)──► InventoryAssignment ◄──(M:1)─── Inventory
 │                      │
 │                      │
 │◄──(AssignedBy)──────┘
 │
 │◄──(ReturnedTo)──────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                                ENUMS                                       │
└─────────────────────────────────────────────────────────────────────────────┘

UserRole:                InventoryStatus:         AssignmentStatus:
- Admin                  - Available              - Active
- NursePractitioner      - Assigned               - Returned
- Staff                  - Reserved               - Expired
                         - Expired                - Lost
                         - Damaged                - Damaged
                         - Disposed
```

---

## Domain Services Interfaces

### Service Layer Interfaces (Domain Layer)

```csharp
public interface IAuthService
{
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto loginDto);
    Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
    Task<ApiResponse<bool>> LogoutAsync(int userId);
    Task<ApiResponse<bool>> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
}

public interface IUserService
{
    Task<ApiResponse<IEnumerable<UserDto>>> GetAllUsersAsync();
    Task<ApiResponse<PagedResult<UserDto>>> GetUsersPagedAsync(int pageNumber, int pageSize);
    Task<ApiResponse<UserDto>> GetUserByIdAsync(int id);
    Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto createUserDto);
    Task<ApiResponse<UserDto>> UpdateUserAsync(int id, UpdateUserDto updateUserDto);
    Task<ApiResponse<bool>> DeleteUserAsync(int id);
}

public interface IInventoryService
{
    Task<ApiResponse<IEnumerable<InventoryDto>>> GetAllInventoriesAsync();
    Task<ApiResponse<PagedResult<InventoryDto>>> GetInventoriesPagedAsync(int pageNumber, int pageSize);
    Task<ApiResponse<InventoryDto>> GetInventoryByIdAsync(int id);
    Task<ApiResponse<InventoryDto>> CreateInventoryAsync(CreateInventoryDto createInventoryDto, int userId);
    Task<ApiResponse<InventoryDto>> UpdateInventoryAsync(int id, UpdateInventoryDto updateInventoryDto);
    Task<ApiResponse<bool>> DeleteInventoryAsync(int id);
    Task<ApiResponse<IEnumerable<InventoryDto>>> GetExpiringInventoriesAsync(int monthsBefore);
    Task<ApiResponse<IEnumerable<InventoryDto>>> GetLowStockInventoriesAsync(int threshold);
}

public interface IInventoryAssignmentService
{
    Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetAllAssignmentsAsync();
    Task<ApiResponse<InventoryAssignmentDto>> CreateAssignmentAsync(CreateInventoryAssignmentDto createAssignmentDto, int assignedByUserId);
    Task<ApiResponse<bool>> ReturnAssignmentAsync(ReturnInventoryAssignmentDto returnAssignmentDto, int returnedToUserId);
    Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetOverdueAssignmentsAsync();
}

public interface IDashboardService
{
    Task<ApiResponse<DashboardStatsDto>> GetDashboardStatsAsync();
    Task<ApiResponse<IEnumerable<InventoryDto>>> GetExpiryAlertsAsync();
    Task<ApiResponse<IEnumerable<InventoryDto>>> GetLowStockAlertsAsync();
    Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetOverdueAlertsAsync();
}
```

This domain model provides a comprehensive foundation with rich entities, proper relationships, comprehensive DTOs, and clear service interfaces that support all business requirements of the inventory management system.
