# InventoryManagement — Resolved Security, Architecture & Code-Quality Issues

## 🔴 CRITICAL — Broken auth

**1. JWT signing/validation mismatch — ✅ Resolved.**
- Token generation and bearer validation now share the strongly typed `Jwt` section (`JwtOptions`) and `IJwtSigningKeyProvider`.

**2. Hardcoded fallback signing secret — ✅ Resolved.**
- There is no fallback secret. The signing key is resolved from inline/user-secrets, environment, mounted file, or cloud secret configuration and must be at least 256 bits.

**3. Startup crash from empty JWT key — ✅ Resolved.**
- Options are validated on startup and key resolution fails fast with a clear configuration error; Development supplies a local key override.

## 🟠 Security / Pentest

- **Unauthenticated idempotency table growth — ✅ Resolved.** Middleware runs after authentication, uses `IIdempotencyStore`, bounded response buffering, lock expiry, retention, and background cleanup.
- **Secrets/tokens persisted and replayed by idempotency — ✅ Resolved.** `/api/auth` is excluded by default, so login/refresh responses are not cached or replayed.
- **Information disclosure via exception messages — ✅ Resolved.** `GlobalExceptionHandler` and services return client-safe `ApiResponse` messages while logging details server-side.
- **No auth rate limiting — ✅ Resolved.** `/api/auth/login` and `/api/auth/refresh` use fixed-window rate limiting (`RateLimiting:Auth`) and return 429 when exceeded.
- **Plaintext refresh tokens and password-change persistence — ✅ Resolved.** Only SHA-256 refresh-token hashes are stored; logout and password change revoke refresh tokens.
- **Stale idempotency lock — ✅ Resolved.** `LockExpiresAt` allows abandoned locks to be reclaimed and cleanup purges expired records.
- **Password hashing dependency — ✅ Resolved.** Passwords use Microsoft `PasswordHasher` (PBKDF2-HMAC-SHA256); the third-party password hashing dependency was removed.

## 🟠 Concurrency / async / threading

- **DbContext used concurrently by dashboard endpoints — ✅ Resolved.** Dashboard aggregate endpoints execute queries sequentially against the scoped context.
- **Lost-update / oversell race — ✅ Resolved.** `ConcurrencyToken` on `BaseEntity` is configured as an EF concurrency token and rotated on insert/update; conflicting stock updates return 409.
- **`async` without `await` token helpers — ✅ Resolved.** Token generation is handled by `TokenService` with explicit async key resolution and synchronous refresh-token creation.
- **Missing cancellation propagation — ✅ Resolved.** Controllers, services, repositories, and unit-of-work methods accept and pass `CancellationToken`.

## 🟠 Performance / DB

- **Over-fetch then in-memory `Take` — ✅ Resolved.** Repositories push filtering, ordering, paging, and `Take` into SQL.
- **Pagination without deterministic ordering — ✅ Resolved.** Generic paging requires an `OrderBy` and returns `PagedList<T>`.
- **In-memory search filters — ✅ Resolved.** Inventory search criteria are applied SQL-side.
- **Dashboard query inefficiencies — ✅ Resolved.** Dashboard queries were consolidated/optimized and sequentialized where needed.
- **Expensive rollback pattern — ✅ Resolved.** Assignment create/return uses `IUnitOfWork.ExecuteInTransactionAsync`; manual rollback/reload paths were removed.

## 🟡 Logic / robustness bugs

- **Paged assignment navigation NRE — ✅ Resolved.** Assignment reads include/project required navigation data before DTO mapping.
- **Soft-delete navigation NRE — ✅ Resolved.** Mapping/read paths are null-safe for historical relationships.
- **`GetByEmailAsync` throwing on not found — ✅ Resolved.** It now returns `null` as its nullable contract indicates.
- **Dead credential validation method — ✅ Resolved.** Obsolete credential and assignment repository methods were removed.
- **Return assignment status/quantity robustness — ✅ Resolved.** Return workflows run transactionally and enforce inventory quantity consistency.
- **Ignored token lifetime config — ✅ Resolved.** Access and refresh lifetimes come from `JwtOptions`.

## 🟡 Design / architecture / structure

- **UnitOfWork disposing DI-owned DbContext — ✅ Resolved.** The unit of work no longer disposes the container-owned context.
- **Repository wiring duplication — ✅ Resolved.** Repositories are registered through DI and consumed consistently.
- **Inconsistent validation response shape — ✅ Resolved.** `[ApiController]` automatic validation uses `InvalidModelStateResponseFactory` to return `ApiResponse`.
- **CORS only in Development / HTTPS metadata not env-gated — ✅ Resolved.** CORS is applied in all environments from `Cors`; `RequireHttpsMetadata` is false only in Development.
- **Missing global exception endpoint/handler — ✅ Resolved.** `GlobalExceptionHandler` maps domain/unhandled exceptions to uniform `ApiResponse` bodies.
- **Serilog bootstrap fragility — ✅ Resolved.** Logging uses two-stage Serilog bootstrap plus configuration-driven console and rolling file sinks.
- **Application-to-Infrastructure dependency — ✅ Resolved.** Domain owns options/security abstractions; Infrastructure supplies implementations.

## 🟢 Minor / cleanup

- **Middleware stream handling and response size — ✅ Resolved.** Idempotency buffers responses only up to `Idempotency:MaxCacheableBodyBytes`; larger responses stream through un-cached.
- **Key encoding and minimum strength — ✅ Resolved.** JWT keys use UTF-8 bytes and must be at least 32 bytes.
- **Health checks — ✅ Resolved.** `/health` is mapped.
- **Dead code — ✅ Resolved.** Removed unused repository/service methods and obsolete rollback paths.

---

## Result Pattern Adoption

Application service interfaces and implementations now return `Result<T>` instead of `ApiResponse<T>`. `ApiControllerBase` maps `Success`, `NotFound`, `Conflict`, `Validation`, `Unauthorized`, `Forbidden`, and generic `Failure` results to accurate HTTP status codes while preserving the client-facing `ApiResponse<T>` JSON envelope.

---

## Current status

All previously listed high-priority security, concurrency, performance, and architecture issues are resolved in the current implementation. Continue tracking new findings here as they are discovered.
