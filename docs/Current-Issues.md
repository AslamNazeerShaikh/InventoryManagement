# InventoryManagement — Architecture, Security & Code-Quality Review

## 🔴 CRITICAL — Broken auth (functional + security)

**1. JWT is signed and validated with different keys/issuer/audience → auth is broken end-to-end.**
- `AuthService.GenerateJwtTokenAsync` signs with `_configuration["Jwt:Secret"]`, `["Jwt:Issuer"]`, `["Jwt:Audience"]` (AuthService.cs:188–228).
- `Program.cs` validates with `JwtSettings:Key/Issuer/Audience` (Program.cs:64–87).
- No `Jwt` section exists (only `JwtSettings`), so signing falls back to the **hardcoded** `"your-secret-key-here-make-it-long"`, issuer/audience `"InventoryManagement"`. Validation expects issuer `InventoryManagement.API`, audience `InventoryManagement.Client`. **Every token minted at login fails validation → 401 on all protected endpoints.** This is the #1 bug.

**2. Hardcoded fallback signing secret** (AuthService.cs:189). Anyone can forge tokens once they know it (it's public). Remove the fallback; fail fast if missing.

**3. Startup now crashes.** The uncommitted edit set `JwtSettings:Key` to `""` → `new SymmetricSecurityKey(GetBytes(""))` throws at boot (Program.cs:65,80). Source the key from user-secrets/env and validate non-empty.

## 🟠 Security / Pentest

- **Unauthenticated DoS via idempotency table growth.** `IdempotencyMiddleware` runs *before* `UseAuthentication` (Program.cs:188 vs 192). Any anonymous `POST/PUT/DELETE/PATCH` with an `Idempotency-Key` inserts a DB row (IdempotencyMiddleware.cs:122–135). `ResponseBody` has **no max length** (IdempotentRequestConfiguration.cs) and there's **no TTL/cleanup** → unbounded storage growth. Move middleware after auth; add retention/size caps.
- **Secrets/tokens persisted & replayed.** Idempotent `POST /auth/login` stores the full response body (JWT + refresh token) in `IdempotentRequests.ResponseBody` in plaintext and replays it to anyone re-presenting the key (IdempotencyMiddleware.cs:184–187, 108–119). Exclude auth endpoints / don't buffer sensitive bodies.
- **Information disclosure.** Every service catch returns `$"...: {ex.Message}"` to the client (e.g., AuthService.cs:74, InventoryService.cs:29). Leaks internal details. Log server-side, return generic messages (controllers already do this correctly — the service-level leak overrides them for `BadRequest(result)` paths).
- **No rate limiting / lockout** on `/auth/login` → brute force. Add ASP.NET rate limiting.
- **Refresh tokens stored in plaintext** and **not invalidated on password change** (AuthService.ChangePasswordAsync:148). Hash stored tokens; clear refresh token on password change.
- **Stale idempotency lock.** If the process dies between insert and completion, the record stays `IsCompleted=false` forever → that key returns 409 permanently (no expiry).
- Good: LINQ everywhere → **no SQL injection**; login returns a uniform "Invalid email or password" → **no user enumeration**; registration is Admin-only; non-admin self-update correctly preserves Role/IsAdmin (UsersController.cs:243–253).

## 🟠 Concurrency / async / threading

- **DbContext used concurrently (thread-safety violation).** `DashboardController.GetAlertsSummary`/`GetDashboardOverview` start multiple service tasks *without awaiting* then `Task.WhenAll` (DashboardController.cs:209–224, 279–307). All share one scoped `IUnitOfWork`/`AppDbContext` → EF Core throws *"A second operation was started on this context"*. These two endpoints will fail intermittently. Await sequentially or use a `DbContextFactory` per parallel query.
- **Lost-update / oversell race.** Assignment create/return read `AvailableQuantity`, then write, with no optimistic concurrency token (InventoryAssignmentService.cs:102–133, 273). Two concurrent assignments can both pass the availability check and oversell. Add a `[Timestamp]`/`RowVersion` concurrency token on `Inventory`.
- **`async` without `await`.** `GenerateJwtTokenAsync` / `GenerateRefreshTokenAsync` are marked `async` but never await (AuthService.cs:185,235) → CS1998, pointless state machine. Make them synchronous / return `Task.FromResult`.
- **No `CancellationToken`** anywhere in controllers/services/repos → client disconnects don't cancel DB work.

## 🟠 Performance / DB

- **Over-fetch then in-memory Take.** `DashboardService.GetRecentInventoriesAsync`/`GetRecentAssignmentsAsync` load the **entire** table via `GetAllAsync(...)`, then `OrderByDescending().Take(count)` in memory (DashboardService.cs:77–80, 99–107). Push ordering/limit to SQL.
- **Pagination without `OrderBy`.** Generic `GetPagedAsync` does `Skip/Take` with no ordering (GenericRepository.cs:139–146) → non-deterministic/overlapping pages.
- **In-memory search filters.** `SearchInventoriesAsync` applies expiry filters on an already-materialized `IEnumerable` (InventoryService.cs:311–323); the no-term branch loads all rows first.
- **Chatty stats.** `GetDashboardStatsAsync` issues 8 sequential `CountAsync` round-trips (DashboardService.cs:24–59).
- **`UnitOfWork.Rollback()`** calls `Reload()` per tracked entity = N queries (UnitOfWork.cs:37).

## 🟡 Logic / robustness bugs

- **`GetAssignmentsPagedAsync` throws NRE.** It uses the generic `GetPagedAsync` (no `Include`), then `ToDto()` dereferences `assignment.Inventory.EquipmentName` / `assignment.User.Name` (MappingExtensions.cs:124,128). Lazy loading isn't enabled → **NullReferenceException** → endpoint returns a failure for all data. The paged users/inventory equivalents survive only because they use null-safe `?.`.
- **Soft-delete + navigation NRE.** All entities have `HasQueryFilter(!IsDeleted)`. If an `Inventory`/`User` is soft-deleted while historical assignments exist, `Include(x => x.Inventory/User)` yields `null` → same non-null-safe mapping NREs in history/list endpoints.
- **`UserRepository.GetByEmailAsync` throws instead of returning null** — `FirstOrDefaultAsync(...) ?? throw new ArgumentNullException()` (UserRepository.cs:21) contradicts its `User?` contract and throws a meaningless exception on "not found".
- **`ValidateUserCredentialsAsync` compares BCrypt hashes by equality** (UserRepository.cs:47–52) — broken by design (salted hashes never equal); dead code, remove.
- **`ReturnAssignmentAsync` unconditionally sets `Status = Available`** (InventoryAssignmentService.cs:274), even for Damaged/Expired/Disposed items; no cap ensuring `AvailableQuantity ≤ Quantity`.
- Config values ignored: token lifetime hardcoded `AddHours(1)`/`AddDays(7)` in 3 places despite `DurationInMinutes`/`RefreshTokenDurationInDays` in appsettings.

## 🟡 Design / architecture / structure

- **`UnitOfWork` disposes a container-owned `AppDbContext`** (UnitOfWork.cs:72). EF's `AddDbContext` already manages its lifetime; the same instance is also resolved by the idempotency middleware. Double-dispose is currently harmless but is an ownership smell and a latent `ObjectDisposedException` risk. Don't dispose injected dependencies.
- **Repositories instantiated by hand inside UnitOfWork** (UnitOfWork.cs:16–18) *and* separately registered in DI (Infrastructure/ServiceCollectionExtensions.cs:23–25) → two parallel wiring mechanisms; the DI registrations for repos are effectively unused.
- **`[ApiController]` auto-400 vs manual ModelState.** With `[ApiController]`, invalid models short-circuit to a `ValidationProblemDetails` 400 before your action runs, so the manual `ModelState.IsValid` → `ApiResponse` blocks are unreachable and your error contract is inconsistent (two shapes).
- **CORS only registered in Development** (`app.UseCors` inside the `IsDevelopment` branch, Program.cs:152) → browsers blocked in production. `RequireHttpsMetadata=false` is hardcoded, not env-gated (Program.cs:75).
- **`UseExceptionHandler("/Error")`** but no `/Error` endpoint exists → opaque 500s in production. No global exception-handling middleware/`IExceptionHandler`; instead every method has a repetitive try/catch.
- **Serilog bootstrap** rebuilds configuration manually with `AddJsonFile("appsettings.json")` (non-optional, no base path) → throws if CWD differs (Program.cs:18–24).
- Layering nit: the Application layer depends on `IConfiguration` for JWT settings (AuthService) instead of a typed `JwtOptions` — brittle and the root cause of the key-mismatch bug.

## 🟢 Minor / cleanup

- `StreamReader` in middleware not disposed (IdempotencyMiddleware.cs:165) — harmless (wraps a `using` MemoryStream) but untidy.
- `Encoding.ASCII` for the key (AuthService.cs:188) — use UTF8.
- No health checks; no outbound `HttpClient` usage (so no socket-exhaustion risk — good).
- Duplicate/dead repo method `InventoryAssignmentRepository.ReturnAssignmentAsync` (logic lives in the service).

---

## Top fixes, in priority order
1. **Unify JWT config** into one `JwtOptions` (key/issuer/audience/lifetime) used by both signing and validation; remove the hardcoded fallback; load the key from user-secrets/env and fail fast if missing. *(Fixes the auth outage + startup crash + hardcoded secret.)*
2. **Move `IdempotencyMiddleware` after `UseAuthentication`**, cap `ResponseBody`, add TTL/cleanup + stale-lock expiry, and skip auth endpoints.
3. **Fix DbContext concurrency** in Dashboard endpoints (await sequentially or use `IDbContextFactory`); add a `RowVersion` concurrency token to `Inventory`.
4. **Fix `GetAssignmentsPagedAsync`** (add includes or a projection) and make assignment mapping null-safe.
5. **Push ordering/paging/`Take` into SQL**; add `OrderBy` to all pagination.
6. Stop leaking `ex.Message`; add a global exception handler; remove `GetByEmailAsync` throw and the dead `ValidateUserCredentialsAsync`.

---