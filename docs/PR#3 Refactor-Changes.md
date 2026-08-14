# PR #3 — Security, Architecture & Code‑Quality Hardening (+ Result Pattern)

> **Pull Request:** [AslamNazeerShaikh/InventoryManagement#3](https://github.com/AslamNazeerShaikh/InventoryManagement/pull/3)
> **Branches:** `Develop` → `Main`
> **Scope:** `103 files changed, +7,812 / −5,577 lines`
> **Sources for this document:** the PR diff (`git diff Main..Develop`), the review captured in [`docs/Current-Issues.md`](../Current-Issues.md), and the full engineering session that produced the change.

This document explains **every file and every meaningful change** in PR #3 — what changed, **why** (the problem/cause), **how** (the approach), and the **benefit** — so the PR can be reviewed and audited line‑by‑line. It is organized by layer and then by file. Nothing is skipped: all 103 changed files appear below.

---

## 1. Executive summary

PR #3 remediates a set of critical, high and medium issues discovered in an architecture/security/code‑quality review (see `docs/Current-Issues.md`) and, on top of that, adopts the **Result design pattern** for API endpoints. The headline problems fixed:

| # | Problem (before) | Fix (this PR) |
|---|------------------|---------------|
| 1 | **Auth was completely broken**: tokens were signed with `Jwt:Secret` (a hardcoded fallback) but validated with `JwtSettings:Key` → every token failed validation (401 everywhere). | Single strongly‑typed `JwtOptions` used by both signing and validation. |
| 2 | **Hardcoded/committed JWT signing secret**. | Key resolved via `IJwtSigningKeyProvider` from inline config / user‑secrets / env / mounted file / cloud secret; min 256‑bit; fail‑fast at startup. |
| 3 | **Startup crash** after the key was emptied to `""`. | Options validation (`ValidateOnStart`) + provider fail‑fast with a clear message. |
| 4 | **Unauthenticated DoS + secret leakage** via idempotency middleware running before auth and storing full response bodies (incl. tokens). | Middleware moved after auth, bounded response cache, auth paths excluded, TTL + background cleanup, request‑hash collision detection. |
| 5 | **Information disclosure**: services returned `ex.Message` to clients. | Global `IExceptionHandler` + controlled Result messages; no internal leakage. |
| 6 | **DbContext used concurrently** in dashboard endpoints (`Task.WhenAll` over one context). | Sequential execution. |
| 7 | **Lost‑update / oversell race** on stock. | Provider‑agnostic optimistic concurrency (`ConcurrencyToken`) → HTTP 409 on conflict. |
| 8 | **`GetAssignmentsPagedAsync` NRE** (paged query without includes). | Deterministic paging with includes + null‑safe mapping. |
| 9 | **Over‑fetch + in‑memory paging/sorting/search**. | SQL‑side ordering/paging/`Take`/search + `AsNoTracking`. |
| 10 | **No `CancellationToken`**, dead code, layering inversion, hardcoded token lifetimes, no health checks, no rate limiting, CORS only in Development. | All addressed (details below). |
| 11 | **Third‑party crypto dependency** (`BCrypt.Net-Next`). | Replaced with Microsoft `PasswordHasher` (PBKDF2‑HMAC‑SHA256). |
| 12 | **Controllers guessed HTTP status** from `ApiResponse.IsSuccess` and leaked repetitive try/catch. | **Result design pattern**: services return `Result<T>` with an error category; `ApiControllerBase` maps to the correct HTTP status while keeping the `ApiResponse<T>` body. |

## 2. Commit history (10 commits)

| Commit | Message | Theme |
|--------|---------|-------|
| `0891278` | Security hardening: remove hardcoded JWT key and update vulnerable dependencies | Secrets + deps |
| `7cf56fe` | chore: update package references to version 10.0.11 | Dependency patch |
| `4c2339d` | chore: update SQLitePCLRaw.lib.e_sqlite3 to 2.1.12 (CVE‑2025‑6965 / GHSA‑2m69‑gcr7‑jv3q) | Security patch |
| `f83e9ce` | test: add unit tests for API response and inventory repository | Tests |
| `b1b1bf9` | docs: add Current-Issues.md (review) | Docs |
| `e7a6582` | docs: remove outdated content from Current-Issues.md | Docs |
| `1c68991` | refactor: enhance documentation and add cancellation tokens in service interfaces | XML docs + CT |
| `8355ed9` | refactor: improve code formatting | Formatting |
| `17ca7c9` | refactor: implement Result pattern in service interfaces and update API response handling | Result pattern |
| `a731be0` | refactor: enhance error handling in InventoryAssignmentService and improve response types | Review fixes |

## 3. How to read this document

For each file: **[New]**, **[Modified]** or **[Removed]** tag, a one‑line purpose, then a bulleted list of the concrete changes with the reasoning. Where a change fixes a review item, the item number from §1 is referenced as _(fixes #N)_.

---

# 4. Solution & project files

### `.gitignore` — [Modified]
- Restored the `!Directory.Build.rsp` negation and removed a duplicated macOS bundle block. **Why:** keep the standard `dotnet new gitignore` template intact so directory‑level build defaults aren't ignored. **Benefit:** correct ignore semantics.

### `InventoryManagement.slnx` — [Modified]
- Registered the four test projects (`Domain.Tests`, `Application.Tests`, `Infrastructure.Tests`, `API.Tests`) into the solution. **Why:** the new test projects must build and run with the solution. **Benefit:** `dotnet test` covers the whole tree.

### `src/InventoryManagement.API/InventoryManagement.API.csproj` — [Modified]
- Bumped `Microsoft.AspNetCore.OpenApi`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.EntityFrameworkCore.Design`, `Scalar.AspNetCore` to patched versions _(security/compat)_.
- **Added** `Serilog.Sinks.Console` (bootstrap logger) and **removed** the unused `Serilog.Enrichers.Environment/Thread/Process` packages _(dependency reduction)_.
- Removed the empty `<Folder Include="Controllers\" />` item. **Benefit:** fewer third‑party packages; console + file structured logging.

### `src/InventoryManagement.Application/InventoryManagement.Application.csproj` — [Modified]
- **Removed** the `ProjectReference` to Infrastructure _(fixes layering inversion #10)_ and the `BCrypt.Net-Next`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `System.IdentityModel.Tokens.Jwt` packages.
- **Added** `Microsoft.EntityFrameworkCore` (for `.Include()` query‑shaping in the include builders), `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`.
- **Why:** the Application layer must depend only on Domain abstractions; all JWT/crypto moved behind Domain interfaces implemented in Infrastructure. **Benefit:** clean dependency direction, no third‑party crypto in Application.

### `src/InventoryManagement.Infrastructure/InventoryManagement.Infrastructure.csproj` — [Modified]
- Bumped EF Core packages to `10.0.11`.
- **Removed** `BCrypt.Net-Next`; **added** `Microsoft.Extensions.Identity.Core` (Microsoft PBKDF2 password hashing), `System.IdentityModel.Tokens.Jwt` (token creation), `Microsoft.Extensions.Hosting.Abstractions` (background service), `Microsoft.Extensions.Options`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Configuration.Abstractions`.
- **Why:** Infrastructure now owns crypto/JWT/idempotency‑hosting concerns. **Benefit:** Microsoft‑first dependencies _(fixes #11)_.

### `src/InventoryManagement.API/appsettings.json` — [Modified]
- Removed old `JwtSettings`, `CORS`, `ApiSettings` sections and the flat `Logging` block.
- **Added** structured `Serilog` (Console + rolling File), `ConnectionStrings:DefaultConnection`, `Jwt` (KeySource/Key/KeySecretName/Issuer/Audience/AccessTokenMinutes/RefreshTokenDays/ClockSkewSeconds), `Idempotency`, `Cors`, `RateLimiting:Auth`, `SeedData`. **Why:** single source of truth for all new subsystems _(fixes #1, #4, #10)_. **Benefit:** typed, validated configuration.

### `src/InventoryManagement.API/appsettings.Development.json` — [Modified]
- Replaced the hardcoded production‑looking JWT key with a clearly‑labelled local dev key; added `Cors:AllowedOrigins` and `SeedData` (`AdminEmail`, `AdminPassword=ChangeMe_LocalDev!2026`). **Why:** remove the committed strong‑looking secret and the well‑known `Admin@123` default _(fixes #2)_. **Benefit:** safe local defaults; no shipped default credential.

### `src/InventoryManagement.API/inventory.db` & `src/InventoryManagement.Infrastructure/inventory.db` — [Removed]
- Deleted the committed SQLite databases (binary). **Why:** databases are runtime artifacts regenerated by migrations + seeding; they should never be in source control. **Benefit:** smaller repo, no stale schema drift.

---

# 5. Domain layer (`InventoryManagement.Domain`)

The Domain gained configuration options, security abstractions, a Result type and an idempotency contract; entities/enums/DTOs/exceptions received XML docs, validation attributes and a concurrency token.

## 5.1 Common

### `Common/BaseEntity.cs` — [Modified]
- **Added** `Guid ConcurrencyToken` and full XML docs on every member. **Why:** provider‑agnostic optimistic concurrency — SQLite has no native `rowversion`, so a rotating Guid token is used _(fixes #7)_. **How:** configured as an EF concurrency token and rotated on every insert/update in `AppDbContext`. **Benefit:** concurrent stock writers conflict (409) instead of silently overwriting.

### `Common/PagedList.cs` — [New]
- `readonly record struct PagedList<T>(IReadOnlyList<T> Items, int TotalCount)`. **Why:** repositories return a page + total count computed together in SQL. **Benefit:** deterministic, single‑round‑trip paging; services map to `PagedResult<DTO>` _(fixes #8, #9)_.

### `Common/Result.cs` — [New]
- `ResultErrorType` enum (`None, Failure, Validation, NotFound, Conflict, Unauthorized, Forbidden`), base `Result` and `Result<T>` with factory methods (`Success/Failure/NotFound/Conflict/Validation/Unauthorized/Forbidden`). **Why:** the Result design pattern — express expected business outcomes as data (with an error category) instead of throwing, and let the API map the category to an HTTP status _(fixes #12)_. **Benefit:** correct status codes (404/409/401/…); no exceptions for normal flows; testable.

## 5.2 Configuration

### `Configuration/JwtOptions.cs` — [New]
- Strongly‑typed JWT config: `KeySource` (`Inline/Environment/File/CloudSecret`), `Key`, `KeySecretName`, `Issuer`, `Audience`, `AccessTokenMinutes`, `RefreshTokenDays`, `ClockSkewSeconds`, with DataAnnotations. **Why:** the single source of truth for signing **and** validation _(fixes #1)_ and configurable lifetimes _(fixes #10)_. **Benefit:** eliminates the key/issuer/audience mismatch; supports local and cloud key sources.

### `Configuration/IdempotencyOptions.cs` — [New]
- `MaxKeyLength`, `MaxCacheableBodyBytes`, `LockDuration`, `Retention`, `CleanupInterval`, `CleanupBatchSize`, `ExcludedPathPrefixes` (default `/api/auth`), with DataAnnotations. **Why:** bound and configure the idempotency subsystem _(fixes #4)_. **Benefit:** memory bounds, TTLs, and excluded sensitive routes are all tunable.

### `Configuration/CorsOptions.cs` — [New]
- `AllowedOrigins`, `AllowCredentials`, `PolicyName`. **Why:** CORS must be configuration‑driven and applied in all environments _(fixes #10)_. **Benefit:** browser clients work in production.

## 5.3 Security abstractions

### `Security/IPasswordHasher.cs` — [New]
- `PasswordVerificationOutcome` enum + `Hash`/`Verify`. **Why:** abstract password hashing so Application depends on Domain, not BCrypt _(fixes #11)_. **Benefit:** Microsoft PBKDF2 implementation injected; rehash‑on‑login supported.

### `Security/ITokenService.cs` — [New]
- `AccessToken`/`RefreshToken` result records + `CreateAccessTokenAsync`, `CreateRefreshToken`, `HashRefreshToken`. **Why:** encapsulate all JWT creation and refresh‑token hashing behind an interface. **Benefit:** Application has no direct token library; refresh tokens are stored only as SHA‑256 hashes _(security)_.

### `Security/IDateTimeProvider.cs` — [New]
- `UtcNow`. **Why:** remove hidden static `DateTime.UtcNow` dependencies. **Benefit:** deterministic, testable time (token expiry, idempotency windows, overdue calc).

### `Security/ISecretClient.cs` — [New]
- `GetSecretAsync(name, ct)`. **Why:** pluggable secret resolution (env/file locally, cloud in production). **Benefit:** same code path for local dev and cloud secret stores.

## 5.4 Entities

### `Entities/User.cs` — [Modified]
- XML docs on all members; clarified that `RefreshToken` now stores a **SHA‑256 hash** (not the raw token). **Why:** a leaked DB must not be replayable _(security)_. **Benefit:** documented, safer token storage.

### `Entities/Inventory.cs` — [Modified]
- XML docs; documented the invariant `0 ≤ AvailableQuantity ≤ Quantity`. **Benefit:** clarifies stock semantics enforced by the service layer.

### `Entities/InventoryAssignment.cs` — [Modified]
- XML docs on all members and navigations. **Benefit:** clearer allocation/return model.

### `Entities/IdempotentRequest.cs` — [Modified]
- **Added** `RequestHash`, `CompletedAt`, `LockExpiresAt`, `ExpiresAt` and XML docs describing the lock lifecycle. **Why:** support lock reclaim, TTL and payload‑collision detection _(fixes #4)_. **Benefit:** robust, bounded idempotency records.

## 5.5 Enums

### `Enums/UserRole.cs`, `Enums/InventoryStatus.cs`, `Enums/AssignmentStatus.cs` — [Modified]
- Added XML docs to each enum and member. **Benefit:** self‑documenting domain vocabulary (no behavior change).

## 5.6 Exceptions

### `Exceptions/DomainExceptions.cs` — [Modified]
- XML docs on all exceptions; **added** `ConcurrencyConflictException` (maps to HTTP 409). **Why:** optimistic‑concurrency conflicts need a typed exception the API can map _(fixes #7)_. **Benefit:** consistent 409 handling with a retry hint.

## 5.7 DTOs

### `DTOs/CommonDto.cs` — [Modified]
- XML docs on `ApiResponse<T>`, `PagedResult<T>`, `AuthResponseDto`, `RefreshTokenDto`, `DashboardStatsDto`; **fixed a divide‑by‑zero** in `PagedResult.TotalPages` when `PageSize <= 0`; added `[Required]` to `RefreshTokenDto.RefreshToken`. **Benefit:** robust pagination math; validated refresh requests.

### `DTOs/UserDto.cs` — [Modified]
- **Added** DataAnnotations (`[Required]`, `[EmailAddress]`, `[StringLength]`, `[Compare]`, `[EnumDataType]`) to `CreateUserDto`, `UpdateUserDto`, `LoginDto`, `ChangePasswordDto`; XML docs throughout. **Why:** automatic model validation via `[ApiController]` replaces manual `ModelState` checks _(fixes #5/#10)_. **Benefit:** consistent 400 validation responses; min‑8 password policy; confirm‑password check.

### `DTOs/InventoryDto.cs` — [Modified]
- DataAnnotations + XML docs on `InventoryDto`, `CreateInventoryDto`, `UpdateInventoryDto`, `InventorySearchDto` (lengths, non‑negative price/quantity, page bounds). **Benefit:** validated inventory input.

### `DTOs/InventoryAssignmentDto.cs` — [Modified]
- DataAnnotations + XML docs on assignment DTOs (positive ids/quantities, note lengths, enum validation). **Benefit:** validated assignment input.

## 5.8 Interfaces

### `Interfaces/IGenericRepository.cs` — [Modified]
- Replaced the old `params includes`/untyped API with a read‑optimized contract: `GetByIdAsync`, `FirstOrDefaultAsync`, `ListAsync`, `AnyAsync`, `CountAsync`, `AddAsync`, `AddRangeAsync`, `Update`, `Remove`, `RemoveRange`, and `GetPagedAsync` returning `PagedList<T>` — all with `CancellationToken`, optional `include`/`orderBy` builder delegates and an `asNoTracking` flag. **Why:** enable SQL‑side filtering/ordering/paging/`Take`, tracking‑free reads and deterministic paging _(fixes #8, #9, #10)_. **Benefit:** less memory, fewer round‑trips, cancellable.

### `Interfaces/IUnitOfWork.cs` — [Modified]
- Removed `IDisposable`, `SaveAsync`, `void Rollback`, and manual `Begin/Commit/RollbackTransactionAsync`; **added** `SaveChangesAsync(ct)` (translates concurrency conflicts) and `ExecuteInTransactionAsync<T>(operation, ct)`. **Why:** the UoW must not dispose the DI‑owned context, and transactions should use the provider execution strategy _(fixes #7/#10)_. **Benefit:** no leaked transactions/partial writes; encapsulated retries.

### `Interfaces/IUserRepository.cs` — [Modified]
- Added `CancellationToken`, `IReadOnlyList` returns; **added** `GetByActiveRefreshTokenHashAsync`; **removed** dead `GetByEmailWithRolesAsync`, `ValidateUserCredentialsAsync`, `UpdateLastLoginAsync`, `UpdateRefreshTokenAsync`. **Why:** refresh tokens are looked up by hash; dead/broken methods removed _(fixes #10)_. **Benefit:** lean, correct contract.

### `Interfaces/IInventoryRepository.cs` — [Modified]
- Added `CancellationToken` + `IReadOnlyList`; **removed** the unused `UpdateQuantityAsync`. **Benefit:** consistent async, cancellable API.

### `Interfaces/IInventoryAssignmentRepository.cs` — [Modified]
- Added `CancellationToken` + `IReadOnlyList`; **removed** the dead repo‑level `ReturnAssignmentAsync` (logic lives in the service). **Benefit:** no duplicate/oversell paths.

### `Interfaces/IIdempotencyStore.cs` — [New]
- `IdempotencyBeginStatus` enum, `IdempotencyBeginResult` struct, and `IIdempotencyStore` (`TryBeginAsync`, `CompleteAsync`, `ReleaseAsync`, `PurgeExpiredAsync`). **Why:** decouple the API middleware from EF/persistence _(fixes #4/#10)_. **Benefit:** testable, swappable idempotency store.

### `Interfaces/IAuthService.cs`, `IUserService.cs`, `IInventoryService.cs`, `IInventoryAssignmentService.cs`, `IDashboardService.cs` — [Modified]
- Every method now takes a `CancellationToken` and returns `Result<T>` instead of `ApiResponse<T>`; full XML docs; removed the leaked `GenerateJwtTokenAsync`/`GenerateRefreshTokenAsync`/`ValidateRefreshTokenAsync` from `IAuthService`. **Why:** cancellation propagation + the Result pattern _(fixes #10, #12)_. **Benefit:** cancellable, correctly‑mapped, leak‑free service contracts.

---

# 6. Infrastructure layer (`InventoryManagement.Infrastructure`)

## 6.1 Data

### `Data/AppDbContext.cs` — [Modified]
- Constructors now accept an optional `IDateTimeProvider`; `DbSet`s exposed as expression-bodied `Set<T>()`.
- `OnModelCreating` iterates all entity types and marks `BaseEntity.ConcurrencyToken` as an EF **concurrency token** generically.
- Replaced `UpdateAuditFields` with `ApplyAuditAndConcurrency`, which stamps `CreatedAt`/`UpdatedAt` **and rotates `ConcurrencyToken`** on Added/Modified in both `SaveChanges` and `SaveChangesAsync`.
- **Why:** provider-agnostic optimistic concurrency + deterministic auditing _(fixes #7)_. **How:** rotating the token on every write means a stale writer's conditional `UPDATE ... WHERE ConcurrencyToken = <old>` matches 0 rows -> `DbUpdateConcurrencyException`. **Benefit:** no silent oversell; injected clock for tests.

### `Data/AppDbContextFactory.cs` — [Modified]
- Design-time factory now reads the connection string from the `INVENTORY_CONNECTION` env var (fallback to a local SQLite file) and gains XML docs. **Benefit:** migrations work across environments.

### `Data/Configurations/IdempotentRequestConfiguration.cs` — [Modified]
- `IdempotencyKey` max length raised to **512**; **added** `RequestHash` (64), bounded `ResponseBody` (1 MB), required `LockExpiresAt`; **replaced** the `IX_..._CreatedAt` index with `IX_..._LockExpiresAt` and `IX_..._ExpiresAt`. **Why:** support reclaim/TTL purging and bound stored bodies _(fixes #4)_. **Benefit:** efficient purge queries; defence-in-depth on row size.

### `Data/Configurations/UserConfiguration.cs`, `InventoryConfiguration.cs`, `InventoryAssignmentConfiguration.cs` — [Modified]
- Added class-level XML docs (`<inheritdoc/>` on `Configure`). No mapping/behavior change (the new `ConcurrencyToken` mapping is applied generically in `AppDbContext`). **Benefit:** documented configuration.

## 6.2 Repositories

### `Repositories/GenericRepository.cs` — [Modified]
- Rewritten to the new `IGenericRepository<T>` contract: a `BuildQuery` helper composes `AsNoTracking` -> `include` -> `Where` -> `orderBy` -> `Take`; `GetPagedAsync` computes count + ordered page in SQL and returns `PagedList<T>`; all methods honour `CancellationToken`; removed the reflection-based `GetByIdPredicate` and `UpdateRange`. **Why:** SQL-side execution, tracking-free reads, deterministic paging _(fixes #8, #9)_. **Benefit:** minimal memory, cancellable, correct pages.

### `Repositories/UserRepository.cs` — [Modified]
- `GetByEmailAsync` now **returns null** instead of `throw new ArgumentNullException()` _(fixes a robustness bug #10)_; **added** `GetByActiveRefreshTokenHashAsync` (tracked, filters hash + expiry + active); `GetNursePractitioners`/`GetActiveUsers` use `AsNoTracking` + ordering; **removed** dead `ValidateUserCredentialsAsync`, `GetByEmailWithRolesAsync`, `UpdateLastLoginAsync`, `UpdateRefreshTokenAsync`. **Benefit:** correct null contract, hashed-token lookup, no dead code.

### `Repositories/InventoryRepository.cs` — [Modified]
- All queries `AsNoTracking` + `CancellationToken`; `SearchInventoriesAsync` uses null-safe `field != null && field.Contains(term)` translated to SQL; **removed** `UpdateQuantityAsync`. **Benefit:** SQL-side search, no client-eval, cancellable.

### `Repositories/InventoryAssignmentRepository.cs` — [Modified]
- Introduced a shared `ReadQuery()` (AsNoTracking + common includes); all list methods return `IReadOnlyList` with `CancellationToken`; **removed** the dead `ReturnAssignmentAsync`. **Benefit:** consistent eager-loaded, cancellable reads; no duplicate return logic.

### `Repositories/UnitOfWork.cs` — [Modified]
- Repositories are now **DI-injected** (single wiring) instead of `new`-ed; **no longer disposes** the DI-owned `DbContext` (dropped `IDisposable`); `SaveChangesAsync` catches `DbUpdateConcurrencyException` and throws `ConcurrencyConflictException`; **added** `ExecuteInTransactionAsync` using `Database.CreateExecutionStrategy()` + explicit transaction (commit/rollback). **Why:** ownership correctness, resilient transactions, concurrency translation _(fixes #7/#10)_. **Benefit:** no double-dispose, no leaked transactions, 409 on conflict.

## 6.3 Security implementations

### `Security/SystemDateTimeProvider.cs` — [New]
- `IDateTimeProvider` backed by `DateTime.UtcNow`. **Benefit:** the real clock in production; swappable in tests.

### `Security/IdentityPasswordHasher.cs` — [New]
- `IPasswordHasher` implemented with Microsoft `PasswordHasher<object>` (PBKDF2-HMAC-SHA256), mapping `PasswordVerificationResult` -> `PasswordVerificationOutcome` (incl. rehash hint). **Why:** replace BCrypt with an in-box Microsoft algorithm _(fixes #11)_. **Benefit:** salted, adaptive hashing; transparent upgrades.

### `Security/EnvironmentFileSecretClient.cs` — [New]
- Default `ISecretClient`: resolves a secret name to a mounted **file**'s contents or an **environment variable**. **Why:** local/self-hosted secret resolution; replaceable by a cloud client. **Benefit:** works in Docker/K8s; cloud-ready.

### `Security/JwtSigningKeyProvider.cs` — [New]
- `IJwtSigningKeyProvider` resolving the symmetric key from `JwtOptions.KeySource` (Inline/Env/File/Cloud), **thread-safe once-only caching** via `SemaphoreSlim`, and **>= 256-bit** enforcement (throws otherwise). **Why:** single, validated signing key shared by signing and validation _(fixes #1, #2, #3)_. **Benefit:** no hardcoded secret; fail-fast on weak/missing keys.

### `Security/TokenService.cs` — [New]
- `ITokenService`: signs JWT access tokens (issuer/audience/lifetime from `JwtOptions`, key from the provider) and creates 256-bit random refresh tokens returning `(RawValue, SHA-256 Hash, ExpiresAt)`; `HashRefreshToken` for deterministic lookup. **Why:** centralize token concerns; store only hashes _(fixes #1, #10, security)_. **Benefit:** consistent claims, config-driven lifetimes, non-replayable stored tokens.

## 6.4 Idempotency

### `Idempotency/EfIdempotencyStore.cs` — [New]
- Implements `IIdempotencyStore`: `TryBeginAsync` (insert lock / replay / in-progress 409 / reclaim expired lock / key-mismatch), `CompleteAsync` (capture response + set retention), `ReleaseAsync` (set-based `ExecuteDelete`), `PurgeExpiredAsync` (bounded batch delete). Concurrent same-key inserts are resolved by PK uniqueness (`DbUpdateException` -> InProgress, failed entity detached). **Why:** robust, race-safe idempotency persistence _(fixes #4)_. **Benefit:** correct replay/lock semantics without leaking a bad EF entity state.

### `Idempotency/IdempotencyCleanupService.cs` — [New]
- `BackgroundService` using a `PeriodicTimer` to purge expired records in bounded batches inside a fresh DI scope; never throws out of the loop; stops on shutdown. **Why:** prevent unbounded table growth _(fixes #4)_. **Benefit:** self-maintaining store; resilient to transient failures.

## 6.5 DI wiring & seeding

### `Extensions/ServiceCollectionExtensions.cs` — [Modified]
- `AddInfrastructureServices` now registers: `IDateTimeProvider`, `IPasswordHasher`, `ISecretClient` (via `TryAddSingleton` so it's overridable), `IJwtSigningKeyProvider`, `ITokenService`, the three repositories, `IUnitOfWork`, `IIdempotencyStore`, and the hosted `IdempotencyCleanupService`; throws if `DefaultConnection` is missing.
- `SeedDatabaseAsync` now uses `MigrateAsync` (not `EnsureCreated`), hashes the seed admin password with `IPasswordHasher`, sources email/password from `SeedData` config, and **generates + logs a random password** when none is configured (no `Admin@123`). **Why:** correct DI graph + safe seeding _(fixes #2, #10, #11)_. **Benefit:** migrations applied, Microsoft-hashed admin, no shipped default credential.

## 6.6 Migrations

### `Migrations/20260813170234_SecurityAndConcurrencyHardening.cs` and `Migrations/20260813170234_SecurityAndConcurrencyHardening.Designer.cs` — [New]
- Adds `ConcurrencyToken` to `Users`/`Inventories`/`InventoryAssignments`; adds `RequestHash`/`CompletedAt`/`LockExpiresAt`/`ExpiresAt` to `IdempotentRequests`; drops `IX_..._CreatedAt`, adds `IX_..._LockExpiresAt` and `IX_..._ExpiresAt`. **Why:** persist the concurrency + idempotency schema changes _(fixes #4, #7)_. **Benefit:** reproducible schema migration.

### `Migrations/AppDbContextModelSnapshot.cs` — [Modified]
- Regenerated to reflect the new columns/indexes. **Benefit:** EF model snapshot stays consistent.

---

# 7. Application layer (`InventoryManagement.Application`)

All services were rewritten to: accept `CancellationToken`, return `Result<T>` (with proper error categories), stop leaking `ex.Message` (unexpected faults bubble to the global handler), use the new repository/UoW APIs, and use `ITokenService`/`IPasswordHasher`/`IDateTimeProvider`.

### `Services/AuthService.cs` — [Modified]
- Removed the direct `IConfiguration`, `JwtSecurityTokenHandler`, `BCrypt`, `SymmetricSecurityKey` usage and the leaky `GenerateJwtTokenAsync`/`GenerateRefreshTokenAsync`/`ValidateRefreshTokenAsync`. Now depends on `IUnitOfWork`, `ITokenService`, `IPasswordHasher`, `IDateTimeProvider`, `ILogger`.
- `LoginAsync`: verifies with `IPasswordHasher`, returns `Result.Unauthorized("Invalid email or password")` for both missing user and bad password (no account enumeration), `Unauthorized("Account is disabled")` for inactive; rehashes on `SuccessRehashNeeded`; issues tokens and persists the **refresh‑token hash** + `LastLoginAt`.
- `RefreshTokenAsync`: looks up the user by **hashed** presented token; rotates tokens.
- `LogoutAsync`/`ChangePasswordAsync`: clear the refresh token (revoke sessions); `ChangePassword` verifies current password and revokes on change; not‑found -> `Result.NotFound`, wrong current password -> `Result.Validation`.
- **Why:** fixes broken auth, secret storage, enumeration and message leakage _(fixes #1, #2, #5, #10, #11, #12)_. **Benefit:** correct, secure authentication with proper status mapping.

### `Services/UserService.cs` — [Modified]
- Uses `IPasswordHasher` for create; email uniqueness -> `Result.Conflict`; not‑found -> `Result.NotFound`; `AsNoTracking` list/paging via the new repo APIs; `CancellationToken` throughout; removed broad `try/catch(ex.Message)`. **Benefit:** leak‑free, correctly‑mapped user management.

### `Services/InventoryService.cs` — [Modified]
- SQL‑side search predicate (single translated query, no in‑memory filtering); available‑quantity delta guard on update; `LoadWithCreatorAsync` helper for tracking‑free DTO reload; duplicates -> `Conflict`, not‑found -> `NotFound`, invalid quantity -> `Validation`; `CancellationToken` throughout. **Why:** _(fixes #5, #9, #12)_. **Benefit:** efficient search, consistent invariants and statuses.

### `Services/InventoryAssignmentService.cs` — [Modified]
- Create/return/update run inside `ExecuteInTransactionAsync`; the paged query now **includes** the related graph (fixes the prior NRE #8); mapping is null‑safe.
- **Return logic fixed:** stock is restored with `Math.Min(Quantity, Available + AssignedQuantity)` and the item only flips `Assigned -> Available` (never overrides Damaged/Expired/Disposed).
- **Update stock‑leak fixed (review):** a terminal status change (e.g. `Returned`) via update is now rejected (`Validation`), forcing returns through the reconciling return endpoint — previously it flipped status without crediting stock.
- **Typed transactional outcomes (review):** the `TransactionOutcome` carries a `ResultErrorType`, so missing‑entity cases map to **404** and business‑rule violations to **400** consistently.
- **Why:** _(fixes #8, plus the High‑severity stock‑leak and status‑mapping items from the code review)_. **Benefit:** correct stock accounting under concurrency; accurate HTTP semantics.

### `Services/DashboardService.cs` — [Modified]
- Consolidated, **sequential** counts (no concurrent DbContext use); recent lists use SQL‑side `OrderByDescending` + `Take` instead of loading whole tables; `CancellationToken` throughout. **Why:** _(fixes #6, #9)_. **Benefit:** no DbContext races; far less memory.

### `Mapping/MappingExtensions.cs` — [Modified]
- Assignment `ToDto` is now **null‑safe** (`assignment.Inventory?.EquipmentName ?? ""`, `assignment.User?.Name ?? ""`); XML docs added. **Why:** soft‑deleted/absent navigations must not NRE during projection _(fixes #8)_. **Benefit:** robust DTO projection.

---

# 8. API layer (`InventoryManagement.API`)

### `Program.cs` — [Modified]
- Rewrote startup: **two‑stage Serilog** (bootstrap logger, then full config via `ReadFrom.Configuration/Services`); registers Infrastructure + Application + `AddApiServices`; ordered pipeline — `UseExceptionHandler` first, Scalar/OpenApi in Dev, `UseSerilogRequestLogging`, `UseHttpsRedirection`, `UseRouting`, `UseCors`, `UseRateLimiter`, `UseAuthentication`, `UseAuthorization`, **`IdempotencyMiddleware` after auth**, `MapControllers`, `MapHealthChecks("/health")`; awaits `SeedDatabaseAsync`; returns an exit code.
- **Why:** fixes middleware ordering, adds global error handling, CORS/rate‑limit/health, and robust logging bootstrap _(fixes #4, #5, #10)_. **Benefit:** correct, observable, secure pipeline.

### `Infrastructure/ApiControllerBase.cs` — [New]
- Base controller implementing the **Result -> HTTP** mapping: `HandleResult<T>(Result<T>)` -> 200 or the mapped error status; a create overload -> 201 via a success factory; `MapStatusCode` maps `NotFound->404`, `Conflict->409`, `Validation->400`, `Unauthorized->401`, `Forbidden->403`, else `400`. The response body remains the `ApiResponse<T>` envelope. **Why:** centralize status selection for the Result pattern _(fixes #12)_. **Benefit:** correct codes, DRY controllers, unchanged wire contract.

### `Infrastructure/ApiServiceCollectionExtensions.cs` — [New]
- `AddApiServices` binds + validates `JwtOptions`/`IdempotencyOptions`/`CorsOptions` (`ValidateDataAnnotations().ValidateOnStart()`); configures the `InvalidModelStateResponseFactory` -> `ApiResponse`; registers the global exception handler + ProblemDetails; CORS (all environments); JWT bearer wired to `IJwtSigningKeyProvider` via `Configure<IJwtSigningKeyProvider>` (no second container); authorization policies; fixed‑window rate limiting on the `auth` policy; health checks. **Why:** the presentation composition root for all the new subsystems _(fixes #1, #3, #5, #10)_. **Benefit:** validated config, correct JWT validation, rate limiting, health.

### `Infrastructure/ErrorHandling/GlobalExceptionHandler.cs` — [New]
- `IExceptionHandler` mapping domain exceptions to status codes (404/409/403/422/400) and unknown errors to 500, returning `ApiResponse`; treats client‑aborted requests as info; logs with structured context. **Why:** stop leaking `ex.Message`; single error contract _(fixes #5)_. **Benefit:** safe, uniform error responses.

### `Infrastructure/IdempotencyMiddleware.cs` — [New]
- Rewritten middleware using `IIdempotencyStore`: handles only mutating methods with an `Idempotency-Key`; enforces `MaxKeyLength`; hashes the request body (with `EnableBuffering` + rewind); begins the operation and returns 409 (in‑progress) / 422 (key mismatch) / replays completed responses (adds `Idempotency-Replayed: true`); buffers the response to capture the outcome, caches only within `MaxCacheableBodyBytes`, releases the lock on 5xx or unhandled exception; always restores `Response.Body`; excludes configured prefixes (auth). **Why:** secure, robust idempotency _(fixes #4)_. **Benefit:** no anonymous DoS/secret replay; correct retry semantics.

### `Middleware/IdempotencyMiddleware.cs` — [Removed]
- The old middleware (ran before auth, resolved `AppDbContext` directly, stored full unbounded bodies including tokens, had a permanent stale‑lock bug). **Why:** replaced by the store‑backed version above _(fixes #4)_. **Benefit:** removes the vulnerable implementation.

### `Controllers/AuthController.cs` — [Modified]
- Extends `ApiControllerBase`; endpoints thread `CancellationToken`, use `HandleResult`; login/refresh decorated with `[EnableRateLimiting("auth")]`; removed the leaky `try/catch` and manual `ModelState` blocks; `GetCurrentUser` returns the claims envelope. **Benefit:** 401 on bad creds, rate‑limited auth, no leakage.

### `Controllers/UsersController.cs` — [Modified]
- Extends `ApiControllerBase`; `HandleResult` + `CancellationToken`; preserved authorization (Admin‑only ops; self‑profile access; non‑admins can't self‑escalate role/admin/provider; can't delete self); create returns 201. **Benefit:** correct statuses, preserved security, DRY.

### `Controllers/InventoryController.cs` — [Modified]
- Extends `ApiControllerBase`; `HandleResult` + `CancellationToken`; clamps `expiring` months and passes `low-stock` threshold; create returns 201; removed leaky try/catch + manual ModelState. **Benefit:** 404 on missing, 409 on duplicate, cancellable.

### `Controllers/InventoryAssignmentsController.cs` — [Modified]
- Extends `ApiControllerBase`; `HandleResult` + `CancellationToken`; preserved ownership checks (recipient or Admin/Provider) — on failure it maps via `HandleResult` and only dereferences `result.Value` on success (no NRE); create returns 201. **Benefit:** correct statuses + preserved authorization.

### `Controllers/DashboardController.cs` — [Modified]
- Extends `ApiControllerBase`; `HandleResult` + `CancellationToken`; the `alerts/summary` and `overview` aggregates now **await sequentially** and read `Result.Value` (fixes the concurrent‑DbContext bug #6); permission‑gated assignment/overdue data preserved. **Benefit:** no DbContext races; correct aggregation.

### `InventoryManagement.API.csproj` — [Modified]
- (See §4) package bumps, `Serilog.Sinks.Console` added, unused enrichers removed, empty Controllers folder item removed.

---

# 9. Tests (`tests/`)

### `InventoryManagement.Domain.Tests` — [New project]
- `InventoryManagement.Domain.Tests.csproj` (xUnit + coverlet, references Domain).
- `EntityDefaultsTests.cs` — entity default values and `BaseEntity.CreatedAt` recency.
- `ApiResponseTests.cs` — `ApiResponse.Success/Failure` and `PagedResult` computed properties (incl. the divide‑by‑zero guard).
- `DomainExceptionsTests.cs` — domain exception message formatting.
- **Benefit:** locks in domain contracts.

### `InventoryManagement.Infrastructure.Tests` — [New project]
- `InventoryManagement.Infrastructure.Tests.csproj` (EF Sqlite in‑memory, references Infrastructure).
- `SqliteInMemoryFixture.cs` — shared in‑memory SQLite context, plus `NewContext()` for cross‑context concurrency tests.
- `InventoryRepositoryTests.cs` — add/retrieve, available filter, barcode exists, low‑stock threshold.
- `AppDbContextTests.cs` — audit stamping on add/modify.
- `SecurityTests.cs` — `IdentityPasswordHasher` (salted, verify success/fail) and `TokenService` (access token structure/expiry, deterministic refresh‑hash, unique refresh values).
- `EfIdempotencyStoreTests.cs` — begin/replay/in‑progress/mismatch/lock‑reclaim/purge.
- `ConcurrencyTests.cs` — stale‑writer `DbUpdateConcurrencyException` and token rotation.
- **Benefit:** verifies the security, idempotency and concurrency machinery.

### `InventoryManagement.API.Tests` — [New project]
- `InventoryManagement.API.Tests.csproj` (references API; `FrameworkReference` AspNetCore.App).
- `Fakes/FakeDashboardService.cs` — hand‑rolled fake returning `Result<T>` (no mocking library).
- `DashboardControllerTests.cs` — Ok + `ApiResponse` body; count pass‑through.
- **Benefit:** controller/Result‑mapping coverage without heavyweight mocks.

### `InventoryManagement.Application.Tests` — [Modified project + New tests]
- `InventoryManagement.Application.Tests.csproj` now references Application + Infrastructure + EF Sqlite + logging abstractions.
- `InventoryAssignmentServiceTests.cs` (new) — the **regression tests for the stock‑leak fix**: update‑to‑`Returned` is rejected and stock/status are unchanged; the return endpoint restores stock within `Quantity`; missing assignment update -> `NotFound`.
- **Benefit:** the High‑severity review bug is now regression‑proof.

---

# 10. Documentation (`docs/`)

### `docs/Current-Issues.md` — [New, then updated]
- Added the original architecture/security/code‑quality review, later rewritten as a **resolved changelog** annotating each item with how it was fixed (and the Result‑pattern adoption). **Benefit:** an auditable record of problems -> fixes.

### `docs/README.md` — [Modified]
- Result pattern overview + status‑code mapping table; PBKDF2 (not BCrypt); new `Jwt`/`Cors`/`Idempotency`/`RateLimiting`/`SeedData` config; local dev credentials (`ChangeMe_LocalDev!2026`, not `Admin@123`); JWT key sourcing (inline/user‑secrets/env/file/cloud).

### `docs/Domain-Model.md` — [Modified]
- Documented `Result`/`Result<T>`; service interfaces now return `Result<T>`; `BaseEntity.ConcurrencyToken`; hashed refresh token.

### `docs/Database-Schema.md` — [Modified]
- Added `ConcurrencyToken` columns and the expanded `IdempotentRequests` schema/indexes; PBKDF2 hash note; Result‑driven conflict/not‑found behavior.

### `docs/Data-Flow-Architecture.md` — [Modified]
- Updated request flow, Result‑to‑HTTP mapping, JWT and idempotency flows, and service examples.

### `docs/API-Testing-Guide.md` — [Modified]
- Updated auth/token testing, response/status guide, and sample credentials.

---

# 11. Status‑code mapping reference (Result pattern)

| `ResultErrorType` | HTTP status | Typical trigger |
|---|---|---|
| `None` (success) | 200 OK (201 Created for create endpoints) | Successful operation |
| `Validation` | 400 Bad Request | Invalid input / precondition (also automatic model validation) |
| `Unauthorized` | 401 Unauthorized | Bad credentials / invalid‑expired refresh token |
| `Forbidden` | 403 Forbidden | Authenticated but not permitted |
| `NotFound` | 404 Not Found | Missing entity |
| `Conflict` | 409 Conflict | Duplicate key / optimistic‑concurrency conflict |
| `Failure` | 400 Bad Request | Generic business‑rule violation |
| _(unhandled exception)_ | 500 (or mapped domain status) via `GlobalExceptionHandler` | Unexpected fault (generic message, no leak) |

Rate‑limited auth endpoints additionally return **429 Too Many Requests**. The JSON **body is always the `ApiResponse<T>` envelope**, unchanged from before this PR.

---

# 12. Complete file index (all 103 files)

**New (37):** `Domain/Common/PagedList.cs`, `Domain/Common/Result.cs`, `Domain/Configuration/{JwtOptions,IdempotencyOptions,CorsOptions}.cs`, `Domain/Security/{IPasswordHasher,ITokenService,IDateTimeProvider,ISecretClient}.cs`, `Domain/Interfaces/IIdempotencyStore.cs`, `Infrastructure/Security/{SystemDateTimeProvider,IdentityPasswordHasher,EnvironmentFileSecretClient,JwtSigningKeyProvider,TokenService}.cs`, `Infrastructure/Idempotency/{EfIdempotencyStore,IdempotencyCleanupService}.cs`, `Infrastructure/Migrations/20260813170234_SecurityAndConcurrencyHardening.cs` (+`.Designer.cs`), `API/Infrastructure/{ApiControllerBase,ApiServiceCollectionExtensions,IdempotencyMiddleware}.cs`, `API/Infrastructure/ErrorHandling/GlobalExceptionHandler.cs`, plus the 4 test projects and their files (`Domain.Tests`, `Application.Tests` additions, `Infrastructure.Tests`, `API.Tests`), and `docs/Current-Issues.md`, `docs/PRs/PR-3-Security-Architecture-Refactor.md`.

**Removed (3):** `API/Middleware/IdempotencyMiddleware.cs`, `API/inventory.db`, `Infrastructure/inventory.db`.

**Modified (rest):** `.gitignore`, `InventoryManagement.slnx`, all 4 `*.csproj`, both `appsettings*.json`, all 5 controllers, all 5 services, `MappingExtensions.cs`, all Domain entities/enums/DTOs/exceptions and all interfaces, `AppDbContext.cs`, `AppDbContextFactory.cs`, the 4 EF configurations, all repositories, `UnitOfWork.cs`, Infrastructure `ServiceCollectionExtensions.cs`, `AppDbContextModelSnapshot.cs`, `Program.cs`, `API.csproj`, and the 6 `docs/*.md` files.

---

# 13. Verification

- **Build:** full solution compiles with **0 warnings / 0 errors** (Infrastructure builds with `TreatWarningsAsErrors=true`).
- **Tests:** **38 passing** — Domain 11, API 3, Infrastructure 21, Application 3 (incl. the stock‑leak regression tests).
- **Runtime (Development):** app starts, applies migrations, seeds the admin; verified `200` login, `401` bad login, `404` missing entity, `409` duplicate, `201` create, idempotent replay (`Idempotency-Replayed: true`, identical result), and `400` validation — all with the unchanged `ApiResponse<T>` body.
- **Security review:** an independent read‑only review confirmed the JWT/idempotency/concurrency/Result code is sound; the one High‑severity finding (assignment update stock leak) was fixed and covered by tests.

> **Note:** the committed `inventory.db` files were removed in this PR. If your workflow expects a checked‑in database, restore them; otherwise add `*.db*` to `.gitignore`. The database is recreated automatically from migrations + seeding on first run.
