# F‑12 · Duplicate Detection Fix — Change Record & Mind Map

**Audit finding:** [F‑12 in Backend-Anti-Patterns-and-Risk-Audit.md](Backend-Anti-Patterns-and-Risk-Audit.md) (now split into **F‑12a** / **F‑12b**)
**Date:** 2026‑08‑23 · **Branch:** `Contribute` · **Provider at time of change:** SQLite (development)

---

## 1. Requirement, restated precisely

The audit reported one finding: *"check‑then‑act duplicate detection returns HTTP 500 instead of 409."*
Investigation showed the audit was **half wrong**, and that two different defects hide behind one symptom:

| | **F‑12a — `Inventory.Barcode` / `SerialNumber`** | **F‑12b — `User.Email`** |
|---|---|---|
| Unique index existed before? | ❌ **No** — plain non‑unique index | ✅ Yes (`IX_Users_Email`) |
| Behaviour under a concurrent race | **Both inserts succeed → duplicate rows, silently** | Second insert rejected by store → uncaught `DbUpdateException` → **500** |
| Severity | 🔴 **Critical** (silent data corruption) | 🟠 High (wrong status code) |
| Fix needed | Create a real constraint | Translate the store error |

A fix that only mapped `DbUpdateException → 409` (the audit's recommendation) would have fixed F‑12b and left
F‑12a **completely unaddressed**, because there was no constraint to violate.

---

## 2. Mind map — end‑to‑end request/response flow (Rule 5)

```
POST /api/inventory  {name, barcode, serialNumber, …}
   │
   ├─▶ TenantResolutionMiddleware ──▶ ITenantContext.TenantId          (multi-tenant scope established)
   ├─▶ IdempotencyMiddleware      ──▶ replay short-circuit
   ├─▶ Authn/Authz                ──▶ permission "inventory.manage"
   │
   ▼
InventoryController.Create
   │
   ▼
InventoryService.CreateInventoryAsync                     ← Application layer
   │
   ├── (1) FAST PATH pre-check ─────────────────────────────────────────────┐
   │      Inventories.IsBarcodeExistsAsync(barcode)                         │
   │      Inventories.IsSerialNumberExistsAsync(serial)                     │
   │        └─ EntitySet.AnyAsync(...)                                      │
   │             └─ AppDbContext global query filter (applied implicitly):  │
   │                  TenantId == CurrentTenantId && !IsDeleted             │
   │      hit → Result.Conflict("Barcode already exists") → HTTP 409        │
   │      NOTE: advisory only. Cannot prevent a concurrent duplicate.  ─────┘
   │
   ├── (2) AddAsync(entity)
   │
   ▼
UnitOfWork.SaveChangesAsync                               ← Infrastructure layer
   │
   ├─▶ AppDbContext.SaveChangesAsync
   │      └─ ApplyAuditAndConcurrency(): stamp TenantId, rotate ConcurrencyToken
   │
   ├─▶ STORE ENFORCES (2) — the authoritative guard:
   │      UX_Inventories_TenantId_Barcode        UNIQUE
   │      UX_Inventories_TenantId_SerialNumber   UNIQUE
   │        filter: <col> IS NOT NULL AND <col> <> '' AND IsDeleted = 0
   │
   └── exception funnel:
         DbUpdateConcurrencyException ─────────────▶ ConcurrencyConflictException ─▶ 409
         DbUpdateException + dialect says UNIQUE ──▶ DuplicateEntityException    ─▶ 409   ★ NEW
         DbUpdateException (FK / NOT NULL / other) ▶ rethrown untouched          ─▶ 500
   │
   ▼
GlobalExceptionHandler.Map  ──▶ ApiResponse<T>.Failure(message)
   │
   ▼
client/src/lib/api.ts  request()  ──▶ throws ApiError(json.message, 409)
   │                                   (generic non-OK branch — no client change required)
   ▼
UI toast: "Barcode already exists"

  PROVIDER SEAM (the only provider-specific knowledge in the stack)
  ┌───────────────────────────────────────────────────────────────┐
  │ IDatabaseProviderDialect                                      │
  │   ├ IsUniqueConstraintViolation(DbUpdateException)            │
  │   └ BuildUniqueWhenPresentFilter(valueCol, softDeleteCol)     │
  │                                                               │
  │ SQLite  → ext codes 2067 / 1555   "col" IS NOT NULL …  = 0    │
  │ SqlServer→ errors 2601 / 2627     [col] IS NOT NULL …  = 0    │
  │ Postgres→ SQLSTATE 23505          "col" IS NOT NULL … = false │
  │ Unknown → false / null  ⇒ startup logs CRITICAL               │
  └───────────────────────────────────────────────────────────────┘
        ▲                                    ▲
        │ resolved once per context          │ resolved at model build
   UnitOfWork ctor                    AppDbContext.OnModelCreating
```

**Concurrency invariant.** Step (1) is a *latency optimization* that produces a friendly per‑field
message. Step (2) is the *correctness guarantee*. Removing (1) would still be correct; removing (2)
reintroduces F‑12a. This separation is the whole point of the fix.

---

## 3. Plans (Rule 2)

**Plan A — adopted.** Filtered (partial) unique index declared in the EF **model** via
`HasIndex(...).IsUnique().HasFilter(dialect-supplied predicate)`, plus dialect‑driven
`DbUpdateException → 409` translation.
*Why:* a real constraint is the only guarantee under concurrency; declaring it in the model means
both `Migrate()` **and** `EnsureCreated()` (used by tests) get it — a raw‑SQL‑only migration would
leave the test schema unprotected and let a regression pass CI.

**Plan B — fallback if a target provider lacks partial indexes (e.g. MySQL).** Unique index over a
persisted sentinel/computed column that maps "absent" to a per‑row distinct value, so NULL/empty
rows cannot collide. Keeps a real store constraint; costs one extra column.

**Plan C — rejected.** Serialize check+insert inside `ExecuteInTransactionAsync` at
`SERIALIZABLE`/`BEGIN IMMEDIATE`. Rejected: it does not scale to the stated millions‑of‑requests
target (write serialization on a hot table), and leaves the schema itself unconstrained, so any
other writer — a bulk import, a second service, a DBA script — can still admit duplicates.

---

## 4. Change record (Rule 4)

Line numbers are **post‑change**.

### 4.1 Added — `server/InventoryManagement.Infrastructure/Data/Providers/IDatabaseProviderDialect.cs` (new, 32 lines)
The provider seam. Two members only: `IsUniqueConstraintViolation` (L22) and
`BuildUniqueWhenPresentFilter` (L31).
**Benefit vs. before:** previously there was no place for provider‑specific persistence knowledge, so
the alternative was `catch (SqliteException)` inline in `UnitOfWork` — which would have hard‑wired the
data layer to SQLite and silently broken on the planned provider switch.

### 4.2 Added — `server/InventoryManagement.Infrastructure/Data/Providers/DatabaseProviderDialects.cs` (new, 172 lines)
| Lines | Content | Rationale |
|---|---|---|
| L18–24 | Provider‑name constants | Avoids magic strings duplicated across model build + error translation |
| L31–37 | `For(providerName)` factory | Pure function of the provider name ⇒ same dialect resolved in both call sites, no DI wiring needed |
| L59–61 | `CanEnforceUniqueWhenPresent` | Lets startup detect the fail‑open case (§4.6) |
| L89–95 | `SqliteDialect` — ext codes 2067/1555 | Typed against `SqliteException` (already a first‑party dependency, Rule 8) |
| L113–117 | `SqlServerDialect` — errors 2601/2627 | Reads `Number` **reflectively** so recognizing SQL Server errors adds **no `Microsoft.Data.SqlClient` package reference** (Rule 8) |
| L136–141 | `PostgreSqlDialect` — SQLSTATE 23505 | Uses `System.Data.Common.DbException.SqlState` — BCL only, no Npgsql reference |
| L160–166 | `UnknownDialect` | Conservative: never claims a unique violation, emits no SQL ⇒ behaviour identical to pre‑change for unlisted providers |

### 4.3 Modified — `server/InventoryManagement.Domain/Exceptions/DomainExceptions.cs`
**Added** L38–45: `DuplicateEntityException(string entityName, Exception innerException)`.
Existing ctor at L35 **unchanged**.
**Why an overload, not a new exception type:** `DuplicateEntityException` is already mapped to 409 by
`GlobalExceptionHandler` (L82). Adding a `ConflictException` would have created two types with one
meaning. Additive overload ⇒ **no API/ABI break** (Rule 1).
**Security note:** the message deliberately does **not** name the colliding column or value — the
provider exception is kept only as `InnerException` for server‑side logs, so a store error can never
leak schema details to a client.

### 4.4 Modified — `server/InventoryManagement.Infrastructure/Repositories/UnitOfWork.cs`
| Change | Lines | Detail |
|---|---|---|
| Added field | L18 | `private readonly IDatabaseProviderDialect _dialect;` |
| Added resolution | L38 | `DatabaseProviderDialects.For(dbContext.Database.ProviderName)` in ctor |
| **Refactored** | L90, L103–104 | Entity‑name extraction moved out of the concurrency `catch` into `ResolveEntityName` — *removed* the duplicated ternary rather than copy‑pasting it into the new catch |
| **Added** | L92–99 | `catch (DbUpdateException ex) when (_dialect.IsUniqueConstraintViolation(ex))` → `DuplicateEntityException` |

**Ordering is load‑bearing:** `DbUpdateConcurrencyException` derives from `DbUpdateException`, so the
concurrency catch (L88) must stay **above** the new one (L92) or 409‑concurrency would be
misreported as 409‑duplicate. Verified by the existing concurrency tests.
**ABI:** the 11‑argument constructor signature is untouched — all DI and test wiring still binds.

### 4.5 Modified — `server/InventoryManagement.Infrastructure/Data/AppDbContext.cs`
**Added** L93 (call site) and L133–176 (`ApplyUniqueWhenPresentIndexes` + local
`AddUniqueWhenPresentIndex`), creating:
- `UX_Inventories_TenantId_Barcode` (L146)
- `UX_Inventories_TenantId_SerialNumber` (L152)

both `UNIQUE` with filter `<col> IS NOT NULL AND <col> <> '' AND IsDeleted = 0`.

**Placed in `AppDbContext`, not `InventoryConfiguration`:** the predicate needs
`Database.ProviderName`, which an `IEntityTypeConfiguration` cannot see. This follows the file's
existing precedent of centralizing cross‑cutting model rules (concurrency token, global query
filter).
**Three exemptions, each deliberate:**
1. `IS NOT NULL` — most items legitimately have no barcode; also SQL Server treats NULLs as *equal* in a unique index, so an unfiltered index would reject the second NULL row.
2. `<> ''` — the DTO permits empty strings; without this they would collide.
3. `IsDeleted = 0` — makes a barcode reusable after soft delete, matching what the soft‑delete‑scoped pre‑check already implied.

### 4.6 Modified — `server/InventoryManagement.Infrastructure/Extensions/ServiceCollectionExtensions.cs`
**Added** L98 (call) and L149–178 (`WarnIfUniquenessCannotBeEnforced`).
**Problem it closes:** `UnknownDialect` returns `null`, so on an unrecognized provider the unique
index is **silently not created** *and* unique violations are no longer recognized — i.e. the exact
F‑12a corruption returns with **no signal at all**. Absence of an integrity constraint must never be
inferred only from its absence.
**Why log `Critical` rather than throw:** an unlisted provider may still be operationally sound, and
hard‑failing startup could brick a deployment on a provider that merely lacks a dialect entry. The
log names the provider and the exact consequence.

### 4.7 Added — migration `20260823102747_AddInventoryUniqueWhenPresentIndexes`
Two `CreateIndex` calls, `unique: true`, with the filter predicate. `Down` drops both.
⚠️ **Deploy note in `Up`:** applying to a database that already contains duplicates **will fail**
until those rows are reconciled — expected, since no constraint previously existed.

### 4.8 Modified — `server/InventoryManagement.Infrastructure/Data/Configurations/InventoryConfiguration.cs`
**Removed** the stale comment claiming *"uniqueness … is enforced per‑tenant in the application layer"* —
which was the incorrect assumption that caused F‑12a. **Replaced** with a pointer to the real
constraint. The two non‑unique lookup indexes (L65–70) are **kept**: a partial index whose predicate
includes `<> ''` is not provably applicable to a plain `WHERE TenantId=@t AND Barcode=@b` lookup, so
dropping them could regress read plans. Cost: one extra index per column on write.

### 4.9 Tests added
| File | Covers |
|---|---|
| `tests/…Infrastructure.Tests/DuplicateDetectionTests.cs` | Two contexts / one DB, **both passing the pre‑check** → duplicate email/barcode/serial each yield `DuplicateEntityException` **and exactly one row**; NULL and empty exemptions; same barcode in two tenants; reuse after soft delete; a NOT NULL violation still propagates as plain `DbUpdateException` |
| `tests/…Infrastructure.Tests/DatabaseProviderDialectTests.cs` | Per‑provider error‑code recognition **including negatives** (787/1299, 547, 23503); exact filter SQL per provider; `CanEnforceUniqueWhenPresent` truth table incl. MySQL/InMemory/null |
| `tests/…API.Tests/GlobalExceptionHandlerTests.cs` | `DuplicateEntityException` → 409 in `ApiResponse` shape; unexpected exception → 500 **without leaking the message** |

---

## 5. Verification performed (Rule 3 — expert checks first, tests as confirmation)

Unit tests are treated as the *secondary* check. The primary evidence is behaviour of the running system.

| # | Check | Result |
|---|---|---|
| 1 | `dotnet build` (Infrastructure has `TreatWarningsAsErrors`) | **0 warnings, 0 errors** |
| 2 | `dotnet test` — 4 projects | **84/84 passed** (Domain 11, Infrastructure 50, Application 18, API 5) |
| 3 | Migration applied to a clean DB, API boots | ✅ `Inventory Management API started successfully` |
| 4 | **Live race: 14 concurrent `POST /api/inventory`, identical barcode+serial** | **1 × 201, 13 × 409, 0 × 500** |
| 5 | Store‑level evidence for the same run | **24 × `UNIQUE constraint failed`** — pre‑change these inserts would have **succeeded as duplicates**; **18 ×** translated to `DuplicateEntityException`; **0** unhandled |
| 6 | Rows actually persisted for that barcode | **exactly 1** |
| 7 | NULL exemption — 3 creates, no barcode/serial | 201, 201, 201 |
| 8 | Empty‑string exemption — 3 creates, `""` barcode/serial | 201, 201, 201 |
| 9 | Duplicate `POST /api/users` email | **409** (was 500) |
| 10 | **FK violation must not be misclassified** — bad `supplierId` | **500, not 409** ⇒ catch filter is precise |
| 11 | `PUT` to a barcode owned by another row | 409 · same barcode unchanged → 200 |
| 12 | Soft‑delete an item, recreate its barcode | **201** ⇒ exemption works |
| 13 | Stock ops (`receive`, `dispose`) + 8 concurrent `PUT`s on one row | 200s, **no 500** |
| 14 | Regression sweep — 27 authenticated GET endpoints | **all 200** |
| 15 | Client contract (Rule 9) | `api.ts` `request()` already surfaces `json.message` on any non‑OK status ⇒ 409 reaches the UI as a toast with **no client change**; design system, a11y, theming untouched |

---

## 6. Known limitations & follow‑ups (not silently absorbed)

1. **Migration SQL is provider‑stamped.** The generated migration embeds the SQLite predicate
   (`"Barcode" IS NOT NULL … "IsDeleted" = 0`). It is compatible with SQL Server (ANSI quotes, `bit = 0`)
   but **not PostgreSQL**, which needs `= false`. On the planned provider switch, regenerate migrations
   against the target provider (standard EF practice). The **model** is already provider‑agnostic.
2. **Message asymmetry.** Fast path says *"Barcode already exists"*; the rare race path says
   *"Inventory with the same unique value already exists."* Deliberate (no column disclosure), but the
   race‑path message is vaguer. Could be sharpened by re‑running the pre‑check on conflict — one extra
   query on an exceptional path.
3. **`IX_Users_Email` is global and not soft‑delete filtered** — a deleted user's email cannot be
   reused (now a clean 409 instead of a 500). Global scope is **intentional and documented**
   (pre‑auth login must locate a user before the tenant is known). Whether to exempt soft‑deleted rows
   is a product decision; relates to F‑18.
4. **Case sensitivity is provider‑defined.** `Barcode = 'ABC'` vs `'abc'` collide on SQL Server
   (case‑insensitive default collation) but not on SQLite/PostgreSQL. Pre‑check and index agree
   *within* a provider, so there is no TOCTOU gap — but the semantics change on migration. Pin an
   explicit collation when switching.
5. **Pre‑existing, found during validation — not fixed here:**
   - `MigrateAsync` at startup hard‑crashes the API (`[FTL] Application terminated unexpectedly`) if the DB was created by `EnsureCreated` and has no `__EFMigrationsHistory`. Encountered on the local dev DB; resolved by rebuilding it. Operationally fragile — relates to F‑09.
   - `client/src/lib/api.ts` `health()` calls `/api/health`, but `Program.cs` maps `/health` ⇒ the client health check always reports false.
   - An invalid `supplierId` returns **500** rather than 400/422 (no FK pre‑validation). Correctly *not* swallowed by this change, but worth its own finding.
