# Domain Model Documentation - Inventory Management System

## Overview

This document details the domain model structure, including entities, enums, DTOs, configuration options, security abstractions, repository contracts, and relationships within the Domain layer of the Inventory Management System.

## Domain Architecture

The domain layer follows **Domain-Driven Design (DDD)** and clean architecture principles with:

- **Entities** for persisted business state
- **DTOs** for API contracts and automatic validation
- **Result Pattern** (`Result` / `Result<T>`) for expected business outcomes
- **Domain Exceptions** for unexpected/domain exception mapping
- **Repository and Unit of Work Interfaces** for data access abstraction
- **Security Abstractions** (`ITokenService`, `IPasswordHasher`, `ISecretClient`) so Application does not depend on Infrastructure
- **Typed Options** (`JwtOptions`, `IdempotencyOptions`, `CorsOptions`) as configuration contracts

---

## Core Entities

### 1. User Entity

**Namespace**: `InventoryManagement.Domain.Entities`  
**Purpose**: Represents system users with authentication material and role-based access control.

```csharp
public class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsProvider { get; set; }
    public UserRole Role { get; set; } = UserRole.Staff;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public virtual ICollection<InventoryAssignment> AssignedInventories { get; set; }
    public virtual ICollection<Inventory> CreatedInventories { get; set; }
}
```

**Key Features:**

- **Role-based Security**: Admin, NursePractitioner, Staff roles
- **Microsoft Password Hashing**: `PasswordHash` stores PBKDF2-HMAC-SHA256 hashes created by `PasswordHasher`
- **Refresh Token Safety**: `RefreshToken` stores only a SHA-256 hash; raw refresh tokens are returned to clients once
- **Token Revocation**: Refresh tokens are cleared on logout and password change

---

### 2. Inventory Entity

**Namespace**: `InventoryManagement.Domain.Entities`  
**Purpose**: Represents medical equipment and stock.

```csharp
public class Inventory : BaseEntity
{
    public string EquipmentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Barcode { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? Supplier { get; set; }
    public int Quantity { get; set; } = 1;
    public int AvailableQuantity { get; set; } = 1;
    public InventoryStatus Status { get; set; } = InventoryStatus.Available;
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public bool IsExpiryAlertSent { get; set; }
    public int? CreatedByUserId { get; set; }
    public virtual User? CreatedByUser { get; set; }
    public virtual ICollection<InventoryAssignment> Assignments { get; set; }
}
```

**Key Features:**

- **Barcode and Serial Support**: Unique for non-deleted rows when present
- **Quantity Model**: `Quantity` is total owned; `AvailableQuantity` is unassigned stock
- **Optimistic Concurrency**: `ConcurrencyToken` prevents lost updates and overselling
- **Status Management**: Available, Assigned, Reserved, Expired, Damaged, Disposed

---

### 3. InventoryAssignment Entity

**Namespace**: `InventoryManagement.Domain.Entities`  
**Purpose**: Tracks equipment assignments with assignment/return lifecycle.

```csharp
public class InventoryAssignment : BaseEntity
{
    public int InventoryId { get; set; }
    public int UserId { get; set; }
    public int AssignedQuantity { get; set; } = 1;
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ReturnDate { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Active;
    public string? AssignmentNotes { get; set; }
    public string? ReturnNotes { get; set; }
    public int? AssignedByUserId { get; set; }
    public int? ReturnedToUserId { get; set; }
    public virtual Inventory Inventory { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual User? AssignedByUser { get; set; }
    public virtual User? ReturnedToUser { get; set; }
}
```

**Key Features:**

- **Complete Audit Trail**: Tracks recipient, assigner, return handler, dates, and notes
- **Transactional Workflows**: Create/return operations run inside `ExecuteInTransactionAsync`
- **Concurrency Protection**: Conflicting stock updates surface as HTTP 409

---

### 4. IdempotentRequest Entity

**Namespace**: `InventoryManagement.Domain.Entities`  
**Purpose**: Persists idempotency lock and replay state for mutating HTTP requests.

```csharp
public class IdempotentRequest
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestMethod { get; set; } = string.Empty;
    public string RequestPath { get; set; } = string.Empty;
    public string? RequestHash { get; set; }
    public int ResponseStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public string? ResponseContentType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime LockExpiresAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsCompleted { get; set; }
}
```

**Key Features:**

- **Locking**: In-progress requests return 409 for duplicate keys
- **Replay**: Completed responses are replayed with `Idempotency-Replayed: true`
- **Collision Detection**: SHA-256 request-body hash returns 422 for key/payload mismatch
- **Sensitive Exclusion**: `/api/auth` is excluded by default

---

## Base Entity

### BaseEntity Abstract Class

**Purpose**: Provides identity, audit fields, soft delete, and provider-agnostic optimistic concurrency.

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
}
```

**Benefits:**

- **Automatic Auditing**: Timestamps and actor fields
- **Soft Delete**: Global query filters exclude deleted rows
- **Optimistic Concurrency**: `ConcurrencyToken` is configured as an EF concurrency token and rotated on every insert/update
- **SQLite Compatible**: Does not rely on database-native `rowversion`

---

## Enums

### 1. UserRole Enum

```csharp
public enum UserRole { Admin = 1, NursePractitioner = 2, Staff = 3 }
```

### 2. InventoryStatus Enum

```csharp
public enum InventoryStatus { Available = 1, Assigned = 2, Reserved = 3, Expired = 4, Damaged = 5, Disposed = 6 }
```

### 3. AssignmentStatus Enum

```csharp
public enum AssignmentStatus { Active = 1, Returned = 2, Expired = 3, Lost = 4, Damaged = 5 }
```

---

## DTOs (Data Transfer Objects)

DTOs use DataAnnotations and `[ApiController]` automatic validation. Invalid requests return `ApiResponse<object>.Failure("Validation failed", errors)` with HTTP 400.

### 1. User DTOs

#### UserDto

```csharp
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsProvider { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

#### CreateUserDto

```csharp
public class CreateUserDto
{
    [Required, StringLength(100, MinimumLength = 1)] public string Name { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(128, MinimumLength = 8)] public string Password { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsProvider { get; set; }
    [EnumDataType(typeof(UserRole))] public UserRole Role { get; set; } = UserRole.Staff;
}
```

### 2. Authentication DTOs

```csharp
public class LoginDto
{
    [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(128, MinimumLength = 1)] public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}
```

### 3. Inventory DTOs

`InventoryDto` exposes equipment data, `Quantity`, `AvailableQuantity`, `InventoryStatus`, creation data, and alert status. Create/update DTOs validate string lengths, optional fields, non-negative purchase price, and positive quantities.

### 4. Assignment DTOs

`InventoryAssignmentDto` exposes inventory/user names, `AssignedQuantity`, assignment/return dates, `AssignmentStatus`, notes, and audit names. Create/update/return DTOs validate IDs, quantity, status, and note length.

### 5. Dashboard DTOs

`DashboardStatsDto` includes total/available/assigned/expiring/low-stock inventories, total users, active assignments, and overdue assignments.

---

## Common Outcome and DTO Types

### 1. Result / Result<T>

**Purpose**: Represents expected application service outcomes without throwing for normal business failures. Controllers map these outcomes to HTTP status codes while preserving the `ApiResponse<T>` response body.

```csharp
public enum ResultErrorType
{
    None, Failure, Validation, NotFound, Conflict, Unauthorized, Forbidden
}

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Message { get; }
    public ResultErrorType ErrorType { get; }
    public IReadOnlyList<string> Errors { get; }

    public static Result Success(string message = "Operation successful");
    public static Result Failure(string message, IReadOnlyList<string>? errors = null);
    public static Result NotFound(string message);
    public static Result Conflict(string message);
    public static Result Validation(string message, IReadOnlyList<string>? errors = null);
    public static Result Unauthorized(string message);
    public static Result Forbidden(string message);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }
}
```

**HTTP Mapping by `ApiControllerBase`:**

| Result type | HTTP status |
| ----------- | ----------- |
| Success | 200 OK, or 201 Created via create-endpoint success factories |
| NotFound | 404 Not Found |
| Conflict | 409 Conflict |
| Validation | 400 Bad Request |
| Unauthorized | 401 Unauthorized |
| Forbidden | 403 Forbidden |
| Failure | 400 Bad Request |

### 2. ApiResponse<T>

**Purpose**: Standardized API response wrapper.

```csharp
public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
}
```

### 3. PagedResult<T>

**Purpose**: Pagination support for large datasets.

```csharp
public class PagedResult<T>
{
    public IEnumerable<T> Data { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; }
    public bool HasNextPage { get; }
    public bool HasPreviousPage { get; }
}
```

---

## Configuration Options and Security Abstractions

### JwtOptions

```csharp
public sealed class JwtOptions
{
    public JwtKeySource KeySource { get; set; }
    public string? Key { get; set; }
    public string? KeySecretName { get; set; }
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 7;
    public int ClockSkewSeconds { get; set; } = 0;
}
public enum JwtKeySource { Inline, Environment, File, CloudSecret }
```

`JwtOptions` is the single source of truth for token creation and validation. The resolved key must be at least 256 bits.

### Security Interfaces

```csharp
public interface ITokenService
{
    Task<AccessToken> CreateAccessTokenAsync(UserDto user, CancellationToken cancellationToken = default);
    RefreshToken CreateRefreshToken();
    string HashRefreshToken(string rawRefreshToken);
}

public interface IPasswordHasher
{
    string Hash(string password);
    PasswordVerificationOutcome Verify(string hashedPassword, string providedPassword);
}

public interface ISecretClient
{
    Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);
}
```

---

## Domain Constants

### 1. AuthConstants

```csharp
public static class AuthConstants
{
    public static class Roles { public const string Admin = "Admin"; public const string NursePractitioner = "NursePractitioner"; public const string Staff = "Staff"; }
    public static class Claims { public const string UserId = "userId"; public const string Email = "email"; public const string Name = "name"; public const string Role = "role"; public const string IsAdmin = "isAdmin"; public const string IsProvider = "isProvider"; }
    public static class Policies { public const string AdminOnly = "AdminOnly"; public const string AdminOrProvider = "AdminOrProvider"; public const string AllRoles = "AllRoles"; }
}
```

---

## Repository Interfaces

### 1. IGenericRepository<T>

The generic repository is read-optimized: reads default to `AsNoTracking`, filters/order/`Take` run in SQL, all async methods accept `CancellationToken`, and paging requires deterministic ordering.

```csharp
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>>? include = null, bool asNoTracking = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IQueryable<T>>? include = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, int? take = null, bool asNoTracking = true, CancellationToken cancellationToken = default);
    Task<PagedList<T>> GetPagedAsync(int pageNumber, int pageSize, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy, Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IQueryable<T>>? include = null, bool asNoTracking = true, CancellationToken cancellationToken = default);
}
```

### 2. IUnitOfWork

```csharp
public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IInventoryRepository Inventories { get; }
    IInventoryAssignmentRepository InventoryAssignments { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default);
}
```

`SaveChangesAsync` translates EF concurrency failures into `ConcurrencyConflictException`. The unit of work does not dispose the DI-owned `DbContext`.

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

User ──(1:M assigned to)──► InventoryAssignment ◄──(M:1)─── Inventory
 │◄──(AssignedBy, optional)────────┘
 │◄──(ReturnedTo, optional)────────┘
 │──(1:M created)──────────────────► Inventory

IdempotentRequest is independent of BaseEntity and stores HTTP idempotency state.
```

---

## Domain Services Interfaces

Service methods accept `CancellationToken` values from controllers and return `Result<T>`. Expected validation/business outcomes use `Result<T>.Success/Failure/NotFound/Conflict/Validation/Unauthorized/Forbidden`; unhandled/domain exceptions are converted by the global exception handler.

```csharp
public interface IAuthService
{
    Task<Result<AuthResponseDto>> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default);
    Task<Result<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto refreshTokenDto, CancellationToken cancellationToken = default);
    Task<Result<bool>> LogoutAsync(int userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto, CancellationToken cancellationToken = default);
}
```

This domain model provides a secure foundation with clean layering, explicit `Result<T>` outcomes, validated DTOs, typed configuration, provider-agnostic concurrency, and explicit abstractions for data access, tokens, hashing, and secrets.
