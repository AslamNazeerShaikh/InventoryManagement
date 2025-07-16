# Data Flow & Architecture Documentation - Inventory Management System

## System Overview

The Inventory Management System follows **Clean Architecture** principles with clear separation of concerns across four main layers. This document details how data flows through these layers and the interaction patterns between components.

## Architecture Layers

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           PRESENTATION LAYER                               │
│                        (InventoryManagement.API)                           │
├─────────────────────────────────────────────────────────────────────────────┤
│ • Controllers (AuthController, UsersController, etc.)                      │
│ • Authentication & Authorization (JWT, Policies)                           │
│ • Request/Response Mapping                                                 │
│ • Validation & Error Handling                                              │
│ • Logging & Monitoring (Serilog)                                          │
└─────────────────────────────────────────────────────────────────────────────┘
                                      ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│                           APPLICATION LAYER                                │
│                     (InventoryManagement.Application)                      │
├─────────────────────────────────────────────────────────────────────────────┤
│ • Business Logic Services (AuthService, UserService, etc.)                 │
│ • DTO Mapping (Entity ↔ DTO conversions)                                  │
│ • Business Rules Validation                                                │
│ • Cross-cutting Concerns (Caching, etc.)                                  │
│ • Service Interfaces Implementation                                        │
└─────────────────────────────────────────────────────────────────────────────┘
                                      ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│                             DOMAIN LAYER                                   │
│                      (InventoryManagement.Domain)                          │
├─────────────────────────────────────────────────────────────────────────────┤
│ • Domain Entities (User, Inventory, InventoryAssignment)                   │
│ • Business Logic & Domain Rules                                            │
│ • Repository Interfaces                                                    │
│ • Service Interfaces                                                       │
│ • DTOs & Enums                                                            │
│ • Domain Constants                                                         │
└─────────────────────────────────────────────────────────────────────────────┘
                                      ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│                         INFRASTRUCTURE LAYER                               │
│                   (InventoryManagement.Infrastructure)                     │
├─────────────────────────────────────────────────────────────────────────────┤
│ • Data Access (Entity Framework Core, SQLite)                              │
│ • Repository Implementations                                               │
│ • Database Context & Configurations                                        │
│ • External Services Integration                                            │
│ • Dependency Injection Setup                                               │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Data Flow Patterns

### 1. Authentication Flow

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Client    │    │API Layer    │    │Application  │    │Infrastructure│
│  (Request)  │    │(Controller) │    │(Service)    │    │(Repository) │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
       │                   │                   │                   │
       │ POST /auth/login  │                   │                   │
       ├──────────────────►│                   │                   │
       │  LoginDto         │                   │                   │
       │                   │                   │                   │
       │                   │ LoginAsync()      │                   │
       │                   ├──────────────────►│                   │
       │                   │ LoginDto          │                   │
       │                   │                   │                   │
       │                   │                   │ GetByEmailAsync() │
       │                   │                   ├──────────────────►│
       │                   │                   │ email             │
       │                   │                   │                   │
       │                   │                   │ User Entity       │
       │                   │                   │◄──────────────────┤
       │                   │                   │                   │
       │                   │                   │ VerifyPassword()  │
       │                   │                   │ GenerateJWT()     │
       │                   │                   │ UpdateRefreshToken│
       │                   │                   ├──────────────────►│
       │                   │                   │                   │
       │                   │ AuthResponseDto   │                   │
       │                   │◄──────────────────┤                   │
       │                   │                   │                   │
       │ AuthResponseDto   │                   │                   │
       │◄──────────────────┤                   │                   │
       │                   │                   │                   │
```

### 2. Inventory Creation Flow

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Client    │    │API Layer    │    │Application  │    │Infrastructure│
│  (Admin)    │    │(Controller) │    │(Service)    │    │(Repository) │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
       │                   │                   │                   │
       │POST /inventory    │                   │                   │
       ├──────────────────►│                   │                   │
       │CreateInventoryDto │                   │                   │
       │                   │                   │                   │
       │                   │ CreateInventoryAsync()                │
       │                   ├──────────────────►│                   │
       │                   │ CreateInventoryDto│                   │
       │                   │                   │                   │
       │                   │                   │ Validate Barcode  │
       │                   │                   │ Uniqueness        │
       │                   │                   ├──────────────────►│
       │                   │                   │                   │
       │                   │                   │ Map DTO → Entity  │
       │                   │                   │ Set Audit Fields  │
       │                   │                   │                   │
       │                   │                   │ AddAsync()        │
       │                   │                   ├──────────────────►│
       │                   │                   │ Inventory Entity  │
       │                   │                   │                   │
       │                   │                   │ Saved Entity      │
       │                   │                   │◄──────────────────┤
       │                   │                   │                   │
       │                   │                   │ Map Entity → DTO  │
       │                   │                   │                   │
       │                   │ InventoryDto      │                   │
       │                   │◄──────────────────┤                   │
       │                   │                   │                   │
       │ 201 Created       │                   │                   │
       │ InventoryDto      │                   │                   │
       │◄──────────────────┤                   │                   │
       │                   │                   │                   │
```

### 3. Assignment Creation Flow with Business Rules

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Client    │    │API Layer    │    │Application  │    │Infrastructure│
│ (Admin/NP)  │    │(Controller) │    │(Service)    │    │(Repository) │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
       │                   │                   │                   │
       │POST /assignments  │                   │                   │
       ├──────────────────►│                   │                   │
       │CreateAssignmentDto│                   │                   │
       │                   │                   │                   │
       │                   │ CreateAssignmentAsync()               │
       │                   ├──────────────────►│                   │
       │                   │ CreateAssignmentDto│                   │
       │                   │                   │                   │
       │                   │                   │ Validate Inventory│
       │                   │                   │ Availability      │
       │                   │                   ├──────────────────►│
       │                   │                   │ GetByIdAsync()    │
       │                   │                   │                   │
       │                   │                   │ Inventory Entity  │
       │                   │                   │◄──────────────────┤
       │                   │                   │                   │
       │                   │                   │ Check Business    │
       │                   │                   │ Rules:            │
       │                   │                   │ • Not Expired     │
       │                   │                   │ • Sufficient Qty  │
       │                   │                   │ • Status Available│
       │                   │                   │                   │
       │                   │                   │ Validate User     │
       │                   │                   │ Exists & Active   │
       │                   │                   ├──────────────────►│
       │                   │                   │                   │
       │                   │                   │ Begin Transaction │
       │                   │                   ├──────────────────►│
       │                   │                   │                   │
       │                   │                   │ Create Assignment │
       │                   │                   │ Update Inventory  │
       │                   │                   │ Quantity          │
       │                   │                   ├──────────────────►│
       │                   │                   │                   │
       │                   │                   │ Commit Transaction│
       │                   │                   ├──────────────────►│
       │                   │                   │                   │
       │                   │ AssignmentDto     │                   │
       │                   │◄──────────────────┤                   │
       │                   │                   │                   │
       │ 201 Created       │                   │                   │
       │ AssignmentDto     │                   │                   │
       │◄──────────────────┤                   │                   │
       │                   │                   │                   │
```

### 4. Dashboard Data Aggregation Flow

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Client    │    │API Layer    │    │Application  │    │Infrastructure│
│  (User)     │    │(Controller) │    │(Service)    │    │(Repository) │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
       │                   │                   │                   │
       │GET /dashboard/    │                   │                   │
       │overview           │                   │                   │
       ├──────────────────►│                   │                   │
       │                   │                   │                   │
       │                   │ GetDashboardOverview()                │
       │                   ├──────────────────►│                   │
       │                   │                   │                   │
       │                   │                   │ Concurrent Calls: │
       │                   │                   │                   │
       │                   │                   │ GetStatsAsync()   │
       │                   │                   ├─┬─────────────────►│
       │                   │                   │ │                 │
       │                   │                   │ │GetRecentInventories()
       │                   │                   │ ├─────────────────►│
       │                   │                   │ │                 │
       │                   │                   │ │GetExpiryAlerts() │
       │                   │                   │ ├─────────────────►│
       │                   │                   │ │                 │
       │                   │                   │ │GetLowStockAlerts()│
       │                   │                   │ ├─────────────────►│
       │                   │                   │ │                 │
       │                   │                   │ │GetRecentAssignments()
       │                   │                   │ ├─────────────────►│
       │                   │                   │ │                 │
       │                   │                   │ │GetOverdueAlerts()│
       │                   │                   │ └─────────────────►│
       │                   │                   │                   │
       │                   │                   │ Await All Results │
       │                   │                   │ Combine Data      │
       │                   │                   │                   │
       │                   │ DashboardOverview │                   │
       │                   │◄──────────────────┤                   │
       │                   │                   │                   │
       │ Dashboard Data    │                   │                   │
       │◄──────────────────┤                   │                   │
       │                   │                   │                   │
```

---

## Request/Response Patterns

### 1. Successful Request Pattern

```
HTTP Request → Authentication → Authorization → Validation → Business Logic → Data Access → Response

Example: GET /api/inventory/1
┌─────────────────────────────────────────────────────────────────────────────┐
│                              REQUEST PIPELINE                              │
└─────────────────────────────────────────────────────────────────────────────┘

1. HTTP Request Received
   ├─ Extract JWT Token from Authorization header
   ├─ Validate JWT signature & expiration
   └─ Extract user claims (UserId, Role, etc.)

2. Authorization Check
   ├─ Check if user has required policy permissions
   ├─ Validate user is active
   └─ Allow/Deny access

3. Controller Action
   ├─ Model binding & validation
   ├─ Extract route parameters
   └─ Call service layer

4. Service Layer
   ├─ Business logic validation
   ├─ Call repository layer
   └─ Map entity to DTO

5. Repository Layer
   ├─ Query database via EF Core
   ├─ Apply filters (IsActive = true)
   └─ Return entity

6. Response Generation
   ├─ Wrap in ApiResponse<T>
   ├─ Set appropriate HTTP status code
   └─ Return JSON response

Response: 200 OK with InventoryDto
```

### 2. Error Handling Pattern

```
Exception/Validation Error → Error Handler → Standardized Error Response

┌─────────────────────────────────────────────────────────────────────────────┐
│                              ERROR PIPELINE                                │
└─────────────────────────────────────────────────────────────────────────────┘

1. Exception Occurs
   ├─ Domain Exception (Business rule violation)
   ├─ Validation Exception (Invalid input)
   ├─ Authentication Exception (Invalid token)
   ├─ Authorization Exception (Insufficient permissions)
   └─ System Exception (Database error, etc.)

2. Exception Handling
   ├─ Log error with Serilog (including user context)
   ├─ Determine appropriate HTTP status code
   └─ Create standardized error response

3. Error Response Format
   {
     "isSuccess": false,
     "data": null,
     "message": "User-friendly error message",
     "errors": ["Detailed error 1", "Detailed error 2"]
   }

Status Codes:
- 400: Bad Request (Validation errors)
- 401: Unauthorized (Authentication failed)
- 403: Forbidden (Authorization failed)
- 404: Not Found (Resource doesn't exist)
- 500: Internal Server Error (System errors)
```

---

## Data Transformation Patterns

### 1. Entity ↔ DTO Mapping

```csharp
// Service Layer Mapping Pattern
public class InventoryService : IInventoryService
{
    public async Task<ApiResponse<InventoryDto>> GetInventoryByIdAsync(int id)
    {
        try
        {
            // 1. Repository call - returns Entity
            var inventory = await _inventoryRepository.GetByIdAsync(id);

            if (inventory == null)
                return ApiResponse<InventoryDto>.Failure("Inventory item not found");

            // 2. Entity → DTO mapping
            var inventoryDto = inventory.ToDto();

            // 3. Add computed fields
            inventoryDto.IsExpiringSoon = inventory.IsExpiringSoon();
            inventoryDto.IsLowStock = inventory.IsLowStock();

            // 4. Return wrapped response
            return ApiResponse<InventoryDto>.Success(inventoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory {Id}", id);
            return ApiResponse<InventoryDto>.Failure("An error occurred");
        }
    }
}

// Extension Methods for Mapping
public static class MappingExtensions
{
    public static InventoryDto ToDto(this Inventory entity)
    {
        return new InventoryDto
        {
            Id = entity.Id,
            EquipmentName = entity.EquipmentName,
            Description = entity.Description,
            // ... map all properties
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static Inventory ToEntity(this CreateInventoryDto dto)
    {
        return new Inventory
        {
            EquipmentName = dto.EquipmentName,
            Description = dto.Description,
            // ... map all properties
            Status = InventoryStatus.Available,
            IsActive = true
        };
    }
}
```

### 2. Audit Trail Pattern

```csharp
// DbContext Override for Automatic Auditing
public class AppDbContext : DbContext
{
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy = _currentUserService.UserId;
                    entry.Entity.UpdatedBy = _currentUserService.UserId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = _currentUserService.UserId;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

---

## Transaction Management Patterns

### 1. Unit of Work Pattern

```csharp
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IInventoryRepository Inventories { get; }
    IInventoryAssignmentRepository InventoryAssignments { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

// Usage in Service Layer
public async Task<ApiResponse<InventoryAssignmentDto>> CreateAssignmentAsync(
    CreateInventoryAssignmentDto createDto, int assignedByUserId)
{
    await _unitOfWork.BeginTransactionAsync();

    try
    {
        // 1. Validate inventory availability
        var inventory = await _unitOfWork.Inventories.GetByIdAsync(createDto.InventoryId);
        if (!inventory.CanAssign(createDto.QuantityAssigned))
        {
            throw new BusinessException("Insufficient inventory or item expired");
        }

        // 2. Create assignment
        var assignment = createDto.ToEntity();
        assignment.AssignedByUserId = assignedByUserId;
        await _unitOfWork.InventoryAssignments.AddAsync(assignment);

        // 3. Update inventory quantity (if needed)
        inventory.Quantity -= createDto.QuantityAssigned;
        await _unitOfWork.Inventories.UpdateAsync(inventory);

        // 4. Commit transaction
        await _unitOfWork.CommitTransactionAsync();

        return ApiResponse<InventoryAssignmentDto>.Success(assignment.ToDto());
    }
    catch
    {
        await _unitOfWork.RollbackTransactionAsync();
        throw;
    }
}
```

---

## Dependency Injection Flow

### 1. Service Registration Pattern

```csharp
// Program.cs - Service Registration
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();

// Infrastructure Layer - Extension Method
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IInventoryAssignmentRepository, InventoryAssignmentRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}

// Application Layer - Extension Method
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IInventoryAssignmentService, InventoryAssignmentService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
```

---

## Caching Strategy (Future Enhancement)

### 1. Proposed Caching Pattern

```csharp
// Cache-Aside Pattern for Dashboard Stats
public class DashboardService : IDashboardService
{
    private readonly IMemoryCache _cache;
    private readonly string STATS_CACHE_KEY = "dashboard_stats";
    private readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(5);

    public async Task<ApiResponse<DashboardStatsDto>> GetDashboardStatsAsync()
    {
        // 1. Check cache first
        if (_cache.TryGetValue(STATS_CACHE_KEY, out DashboardStatsDto cachedStats))
        {
            return ApiResponse<DashboardStatsDto>.Success(cachedStats);
        }

        // 2. Cache miss - get from database
        var stats = await ComputeStatsFromDatabase();

        // 3. Cache the result
        _cache.Set(STATS_CACHE_KEY, stats, CACHE_DURATION);

        return ApiResponse<DashboardStatsDto>.Success(stats);
    }

    // Cache invalidation on data changes
    public async Task InvalidateDashboardCache()
    {
        _cache.Remove(STATS_CACHE_KEY);
    }
}
```

---

## Performance Optimization Patterns

### 1. Pagination with EF Core

```csharp
public async Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize)
{
    var query = _context.Set<T>().Where(x => x.IsActive);

    var totalCount = await query.CountAsync();

    var items = await query
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new PagedResult<T>
    {
        Data = items,
        TotalCount = totalCount,
        PageNumber = pageNumber,
        PageSize = pageSize
    };
}
```

### 2. Optimized Queries with Projections

```csharp
// Instead of loading full entities
public async Task<IEnumerable<InventoryDto>> GetInventoriesAsync()
{
    return await _context.Inventories
        .Where(i => i.IsActive)
        .Select(i => new InventoryDto
        {
            Id = i.Id,
            EquipmentName = i.EquipmentName,
            // Only select needed fields
        })
        .ToListAsync();
}
```

---

## Security Data Flow

### 1. JWT Token Validation Flow

```
1. Request with Authorization Header
   └─ Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

2. JWT Middleware Validation
   ├─ Verify signature with secret key
   ├─ Check expiration time
   ├─ Validate issuer and audience
   └─ Extract claims (UserId, Role, etc.)

3. Claims Principal Creation
   ├─ Create ClaimsPrincipal with user claims
   ├─ Set HttpContext.User
   └─ Make available to controllers

4. Authorization Policy Evaluation
   ├─ Check required policy (AdminOnly, AdminOrProvider, etc.)
   ├─ Evaluate claims against policy requirements
   └─ Allow/Deny access

5. Controller Access
   ├─ Access user claims via User.FindFirst()
   ├─ Extract UserId for audit trail
   └─ Pass to service layer
```

This comprehensive data flow documentation provides a complete understanding of how the Inventory Management System processes requests, manages data transformations, handles errors, and maintains security throughout the application layers.
