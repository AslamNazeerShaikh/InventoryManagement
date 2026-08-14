# Data Flow & Architecture Documentation - Inventory Management System

## System Overview

The Inventory Management System follows **Clean Architecture** principles with clear separation of concerns across four main layers. This document details how data flows through these layers and the interaction patterns between components after the security and architecture hardening refactor.

## Architecture Layers

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           PRESENTATION LAYER                               │
│                        (InventoryManagement.API)                           │
├─────────────────────────────────────────────────────────────────────────────┤
│ • Controllers (AuthController, UsersController, etc.)                      │
│ • Authentication & Authorization (JWT, Policies)                           │
│ • CORS, Rate Limiting, Health Checks                                       │
│ • Idempotency Middleware (after authentication)                            │
│ • Automatic Model Validation → ApiResponse                                 │
│ • Global Exception Handling → ApiResponse                                  │
│ • Structured Request Logging (Serilog)                                     │
└─────────────────────────────────────────────────────────────────────────────┘
                                      ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│                           APPLICATION LAYER                                │
│                     (InventoryManagement.Application)                      │
├─────────────────────────────────────────────────────────────────────────────┤
│ • Business Logic Services (AuthService, UserService, etc.)                 │
│ • DTO Mapping (Entity ↔ DTO conversions)                                  │
│ • Business Rules Validation via Result<T> outcomes                         │
│ • CancellationToken propagation                                            │
│ • Depends on Domain abstractions only                                      │
└─────────────────────────────────────────────────────────────────────────────┘
                                      ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│                             DOMAIN LAYER                                   │
│                      (InventoryManagement.Domain)                          │
├─────────────────────────────────────────────────────────────────────────────┤
│ • Domain Entities and IdempotentRequest model                              │
│ • Repository, UnitOfWork, Token, Hasher, Secret interfaces                 │
│ • Result types, DTOs, Enums, Options, Constants, Domain Exceptions         │
└─────────────────────────────────────────────────────────────────────────────┘
                                      ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│                         INFRASTRUCTURE LAYER                               │
│                   (InventoryManagement.Infrastructure)                     │
├─────────────────────────────────────────────────────────────────────────────┤
│ • Entity Framework Core + SQLite                                           │
│ • Repository and UnitOfWork Implementations                                │
│ • JWT Signing Key Provider, TokenService, PasswordHasher                   │
│ • Environment/File Secret Client                                           │
│ • EF Idempotency Store and Cleanup Hosted Service                          │
└─────────────────────────────────────────────────────────────────────────────┘
```

The Application layer no longer references Infrastructure. Domain owns the contracts; Infrastructure owns the implementations.

---

## Data Flow Patterns

### 1. Authentication Flow

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Client    │    │API Layer    │    │Application  │    │Infrastructure│
│  (Request)  │    │(Controller) │    │(Service)    │    │(Security/DB)│
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
       │                   │                   │                   │
       │ POST /auth/login  │                   │                   │
       ├──────────────────►│                   │                   │
       │                   │ [RateLimit auth]  │                   │
       │                   │ Validate DTO      │                   │
       │                   │ LoginAsync(ct)    │                   │
       │                   ├──────────────────►│                   │
       │                   │                   │ GetByEmailAsync() │
       │                   │                   ├──────────────────►│
       │                   │                   │ Verify PBKDF2 hash│
       │                   │                   │ CreateAccessToken │
       │                   │                   ├──────────────────►│
       │                   │                   │ Resolve Jwt key   │
       │                   │                   │◄──────────────────┤
       │                   │                   │ Store refresh hash│
       │                   │ AuthResponseDto   │                   │
       │                   │◄──────────────────┤                   │
       │  accessToken + raw refresh token      │                   │
       │◄──────────────────┤                   │                   │
```

**Security notes:**

- JWT generation and validation both use `JwtOptions` and `IJwtSigningKeyProvider`.
- `Jwt:KeySource` supports `Inline`, `Environment`, `File`, and `CloudSecret`.
- Signing keys shorter than 256 bits fail fast.
- Refresh tokens are stored only as SHA-256 hashes and revoked on logout/password change.
- `/api/auth/login` and `/api/auth/refresh` are fixed-window rate-limited and can return HTTP 429.

### 2. Inventory Creation Flow

```
Client (Admin/Provider)
  └─ POST /api/inventory
      └─ JWT + policy check
          └─ Automatic DTO validation
              └─ CreateInventoryAsync(ct)
                  ├─ Validate barcode/serial uniqueness
                  ├─ Map DTO → Entity
                  ├─ Set Quantity and AvailableQuantity
                  ├─ AddAsync + SaveChangesAsync
                  └─ AppDbContext sets audit fields and ConcurrencyToken
```

### 3. Assignment Creation Flow with Business Rules

```
Client (Admin/Provider)
  └─ POST /api/inventoryassignments
      └─ CreateAssignmentAsync(ct)
          └─ ExecuteInTransactionAsync
              ├─ Load inventory/user
              ├─ Check active user, expiry, status, available quantity
              ├─ Create InventoryAssignment
              ├─ Decrease Inventory.AvailableQuantity
              └─ Save with ConcurrencyToken check
```

Conflicting concurrent updates raise `ConcurrencyConflictException`, mapped to HTTP 409 by `GlobalExceptionHandler`.

### 4. Dashboard Data Aggregation Flow

```
GET /api/dashboard/overview
  └─ GetDashboardOverview(ct)
      ├─ await GetDashboardStatsAsync(ct)
      ├─ await GetRecentInventoriesAsync(5, ct)
      ├─ await GetExpiryAlertsAsync(ct)
      ├─ await GetLowStockAlertsAsync(ct)
      └─ if Admin/Provider:
          ├─ await GetRecentAssignmentsAsync(5, ct)
          └─ await GetOverdueAlertsAsync(ct)
```

Dashboard aggregate endpoints intentionally execute sequentially against the scoped DbContext; they no longer use `Task.WhenAll` over the same context.

### 5. Idempotency Flow

```
Mutating request (POST/PUT/PATCH/DELETE) + Idempotency-Key
       │
       ▼
Authentication and authorization complete first
       │
       ▼
Skip excluded prefixes (default: /api/auth)
       │
       ▼
Validate key length and SHA-256 hash request body
       │
       ├─ Existing completed same request → replay response + Idempotency-Replayed: true
       ├─ Existing in-progress unexpired → 409 Conflict
       ├─ Existing key with different method/path/body hash → 422 key mismatch
       └─ New/expired lock → execute request
                                  │
                                  ├─ 5xx response/exception → release lock for retry
                                  ├─ Response <= MaxCacheableBodyBytes → cache for replay
                                  └─ Larger response → stream through, do not cache body
```

`IdempotencyCleanupService` purges completed rows after retention and abandoned locks after their lock window in bounded batches.

---

## Request/Response Patterns

### 1. Successful Request Pattern

```
HTTP Request → Exception Handler → HTTPS/Serilog → Routing → CORS → Rate Limiter
             → Authentication → Authorization → Idempotency (if applicable)
             → Controller → Automatic Model Validation → Business Logic
             → Repository/UnitOfWork → Result<T> → ApiControllerBase
             → ApiResponse<T> body with mapped HTTP status
```

### 2. Result-to-HTTP Mapping Pattern

Application services return `Result<T>` for expected outcomes. `ApiControllerBase.HandleResult(...)` maps the result to the transport status and wraps the same message/data/errors in the stable `ApiResponse<T>` envelope.

| Result outcome | HTTP status | Notes |
| -------------- | ----------- | ----- |
| Success | 200 OK | Create endpoints can use a success factory for 201 Created |
| NotFound | 404 Not Found | Missing entity/resource |
| Conflict | 409 Conflict | Duplicate data or current-state conflict |
| Validation | 400 Bad Request | Business validation/precondition failure |
| Unauthorized | 401 Unauthorized | Invalid credentials/refresh token |
| Forbidden | 403 Forbidden | Authenticated caller lacks permission |
| Failure | 400 Bad Request | Generic business-rule failure |

The HTTP response body remains unchanged for clients:

```json
{
  "isSuccess": false,
  "message": "Inventory not found",
  "data": null,
  "errors": []
}
```

### 3. Error Handling Pattern

```
Exception/Validation Error → GlobalExceptionHandler or ApiBehaviorOptions → ApiResponse
```

**Unhandled/Exception Mapping:**

- 400: Generic domain exception / bad request; generic `Result.Failure`
- 401: `Result.Unauthorized` (for example invalid login)
- 403: Unauthorized operation / `Result.Forbidden`
- 404: Entity not found / `Result.NotFound`
- 409: Duplicate entity, state conflict, or concurrency conflict / `Result.Conflict`
- 422: Insufficient inventory, expired inventory, invalid business operation, idempotency key mismatch
- 429: Auth rate limit exceeded
- 500: Unexpected internal error with generic message

**Error Response Format:**

```json
{
  "isSuccess": false,
  "message": "Validation failed",
  "data": null,
  "errors": ["The Email field is not a valid e-mail address."]
}
```

Services return `Result<T>` for expected failures. Handlers log internal details server-side; unknown errors return `An unexpected error occurred.` to clients.

---

## Data Transformation Patterns

### 1. Entity ↔ DTO Mapping with Result<T>

```csharp
public async Task<Result<InventoryDto>> GetInventoryByIdAsync(
    int id,
    CancellationToken cancellationToken)
{
    var inventory = await _unitOfWork.Inventories.GetByIdAsync(id, cancellationToken);
    if (inventory is null)
    {
        return Result<InventoryDto>.NotFound("Inventory not found");
    }

    return Result<InventoryDto>.Success(inventory.ToDto());
}
```

Mapping extensions expose DTO shapes without password hashes, refresh-token hashes, or concurrency internals.

### 2. Audit and Concurrency Pattern

```csharp
foreach (var entry in ChangeTracker.Entries<BaseEntity>())
{
    if (entry.State == EntityState.Added)
    {
        entry.Entity.CreatedAt = now;
        entry.Entity.ConcurrencyToken = Guid.NewGuid();
    }

    if (entry.State == EntityState.Modified)
    {
        entry.Entity.UpdatedAt = now;
        entry.Entity.ConcurrencyToken = Guid.NewGuid();
    }
}
```

`ConcurrencyToken` is configured as an EF concurrency token for every `BaseEntity`. If another request updates the same row first, EF raises a concurrency exception and the API returns HTTP 409.

---

## Transaction Management Patterns

### 1. Unit of Work Pattern

```csharp
public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IInventoryRepository Inventories { get; }
    IInventoryAssignmentRepository InventoryAssignments { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}
```

Manual `BeginTransactionAsync`/`CommitTransactionAsync`/`RollbackTransactionAsync` calls and per-entity rollback are no longer part of the public unit-of-work API.

---

## Dependency Injection Flow

### 1. Service Registration Pattern

```csharp
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddApiServices(builder.Configuration, builder.Environment);
```

**API registrations include:** controllers with custom validation responses, `ApiControllerBase` Result mapping, `GlobalExceptionHandler`, CORS, JWT bearer authentication, authorization policies, fixed-window auth rate limiting, and health checks.

**Infrastructure registrations include:** `AppDbContext`, repositories, `IUnitOfWork`, `IdentityPasswordHasher`, `TokenService`, `EnvironmentFileSecretClient`, `JwtSigningKeyProvider`, `EfIdempotencyStore`, and `IdempotencyCleanupService`.

---

## Configuration Flow

### JWT Key Resolution

```
JwtOptions → JwtSigningKeyProvider
    ├─ Inline: Jwt:Key
    ├─ Environment: env var named by Jwt:KeySecretName
    ├─ File: mounted file path named by Jwt:KeySecretName
    └─ CloudSecret: ISecretClient.GetSecretAsync(KeySecretName)
```

Both token generation and validation use the same resolved key, issuer, audience, access-token lifetime, refresh-token lifetime, and clock skew. There is no `JwtSettings` section and no hardcoded fallback secret.

### CORS and HTTPS Metadata

- `Cors` is applied in all environments using explicit `AllowedOrigins`.
- `RequireHttpsMetadata` is false only in Development and true elsewhere.

### Logging

Serilog is configured in two stages: a bootstrap logger for startup failures, then full structured configuration from the `Serilog` appsettings section with console and rolling file sinks.

---

## Caching Strategy (Future Enhancement)

Dashboard/data caching remains a future enhancement. Idempotency response replay is not a general cache; it is bounded, key-specific, excludes auth endpoints, and exists only to make client retries safe.

---

## Performance Optimization Patterns

### 1. Pagination with EF Core

```csharp
var page = await repository.GetPagedAsync(
    pageNumber,
    pageSize,
    orderBy: q => q.OrderBy(x => x.Id),
    predicate: x => !x.IsDeleted,
    cancellationToken: cancellationToken);
```

Pagination is deterministic because ordering is mandatory.

### 2. Optimized Queries

- Reads default to `AsNoTracking`.
- Filtering, ordering, paging, and `Take` are pushed into SQL.
- Dashboard recent/alert queries avoid materializing entire tables before filtering.
- Dashboard aggregate endpoints execute sequentially to respect DbContext thread-safety.

---

## Security Data Flow

### 1. JWT Token Validation Flow

```
1. Request with Authorization: Bearer <accessToken>
   └─ Token extracted by JWT bearer middleware

2. JWT Middleware Validation
   ├─ Resolve shared validation key from IJwtSigningKeyProvider
   ├─ Verify HMAC-SHA256 signature
   ├─ Check expiration with Jwt:ClockSkewSeconds
   ├─ Validate Jwt:Issuer
   └─ Validate Jwt:Audience

3. Claims Principal Creation
   ├─ userId, email, name, role, isAdmin, isProvider claims
   ├─ HttpContext.User populated
   └─ Claims available to controllers/services

4. Authorization Policy Evaluation
   ├─ AdminOnly
   ├─ AdminOrProvider
   └─ AllRoles
```

### 2. Password and Refresh Token Flow

```
Password → Microsoft PasswordHasher → PBKDF2-HMAC-SHA256 hash → Users.PasswordHash
Refresh token raw value → returned once to client
Refresh token raw value → SHA-256 hash → Users.RefreshToken
Logout/password change → Users.RefreshToken = null
```

This comprehensive data flow documentation provides a complete understanding of how the Inventory Management System processes requests, manages data transformations, handles errors, and maintains security throughout the application layers.
