# Group 2 Feature Changes — Domain-agnostic backend extensions + full-stack wiring

> Scope: **full-stack** (backend `server/` + client `client/`). Unlike Group 1 (frontend-only),
> Group 2 adds small, **domain-agnostic** backend capabilities and surfaces them in the UI. Every new
> concept is generic (any industry / any SKU): a **stock-movement ledger**, **receive/restock &
> reorder**, **suppliers**, **locations & transfers**, **assignment lifecycle extras** (partial
> returns, renew, condition) and **maintenance/calibration schedules**. The medical framing is only
> the demo.

**Verification (Rule 3):** solution build clean (0 warnings) · `next build` clean (15 routes) ·
53 unit/integration tests pass (Domain 11, Application 18, Infrastructure 21, API 3) · 12 live
end-to-end smoke tests against the running API (receive → adjust → transfer → dispose → ledger,
partial return, renew, maintenance due + recurring completion, reorder list, supplier item-count).

**Rules honored:** R1 (additive only — legacy free-text `Supplier`/`Location` retained, no API/ABI
breaks), R2 (game plan + fallbacks, below), R3 (live verification, not just unit tests), R4 (this
change-log + XML docs on every new type/member), R5 (mind map + scale notes), R6 (pulled latest
`Develop` first), R7 (reused existing patterns/MCP-free), R8 (first-party only — no new dependencies),
R9 (preserved the design system: same UI primitives, tones, tokens, responsiveness, a11y).

---

## 1. Mind map — request/response flow (Rule 5)

```
Client (Next.js)                    ASP.NET API (new/extended)              Application + Infra
────────────────                    ──────────────────────────              ───────────────────
Inventory detail
  Receive  ─────► POST /api/inventory/{id}/receive   ─► InventoryController ─► StockService.Receive ┐
  Adjust   ─────► POST /api/inventory/{id}/adjust    ─►     "            "  ─► StockService.Adjust   │  ExecuteInTransactionAsync
  Transfer ─────► POST /api/inventory/{id}/transfer  ─►     "            "  ─► StockService.Transfer │  → mutate Inventory
  Dispose  ─────► POST /api/inventory/{id}/dispose   ─►     "            "  ─► StockService.Dispose  │  → append StockMovement (ledger)
  Ledger   ─────► GET  /api/inventory/{id}/movements ─►     "            "  ─► StockService.GetMovements
  Reorder  ─────► GET  /api/inventory/reorder        ─► InventoryController ─► InventoryService.GetReorder
Suppliers page ─► CRUD /api/suppliers                ─► SuppliersController ─► SupplierService
Locations page ─► CRUD /api/locations                ─► LocationsController ─► LocationService (cycle guard)
Maintenance    ─► CRUD /api/maintenance              ─► MaintenanceController─► MaintenanceService
  Complete ─────► POST /api/maintenance/{id}/complete ─►    "            "  ─► rolls NextDueAt by IntervalDays
  Due      ─────► GET  /api/maintenance/due?daysAhead ─►    "            "  ─► GetDue (open + overdue)
Assignments
  Return   ─────► POST /api/inventoryassignments/return (ReturnQuantity? + ReturnCondition?)  ─► partial return + ledger
  Renew    ─────► POST /api/inventoryassignments/renew                                         ─► extend + RenewalCount++
  Due-soon ─────► GET  /api/inventoryassignments/due-soon?daysAhead                            ─► upcoming returns
```

**Scale notes (Rule 5):** every stock mutation runs inside `ExecuteInTransactionAsync` and relies on
the existing GUID `ConcurrencyToken` for lost-update/oversell protection (409 on conflict). The
ledger is append-only and indexed on `(InventoryId)`, `(CreatedAt)`, `(MovementType)`. Supplier /
location item-counts use a single grouped SQL query (`GetCountsBySupplierAsync` /
`GetCountsByLocationAsync`) — **no N+1**. New list endpoints support server-side paging.

**Plan A / Plan B (Rule 2):** Plan A modeled stock transfers as per-(item,location) stock rows;
that is a large, breaking model change. **Plan B (chosen)** keeps one `Inventory` row and records a
whole-item **location move** plus a `Transferred` ledger entry — additive, non-breaking, and enough
for the demo. Documented as a future extension.

---

## 2. New backend files

### Domain (`server/InventoryManagement.Domain`)
| File | Purpose |
| --- | --- |
| `Enums/StockMovementType.cs` | Ledger classification: Received, Assigned, Returned, Adjusted, Transferred, Disposed. |
| `Enums/MaintenanceType.cs` | Inspection, Calibration, Service, Repair, Cleaning. |
| `Enums/MaintenanceStatus.cs` | Scheduled, Due, Overdue, Completed, Cancelled. |
| `Enums/ReturnCondition.cs` | Good, Damaged, Lost, NeedsRepair (condition-on-return). |
| `Entities/StockMovement.cs` | Append-only ledger row (type, signed `QuantityChange`, `BalanceAfter`, reason, actor, links to assignment/from-to location/supplier, unit cost). |
| `Entities/Supplier.cs` | Managed vendor (name, contacts, `LeadTimeDays`, `IsActive`). |
| `Entities/Location.cs` | Managed location/bin with self-referencing hierarchy (`ParentLocationId`). |
| `Entities/MaintenanceSchedule.cs` | Service/calibration schedule (type, `IntervalDays`, `LastPerformedAt`, `NextDueAt`, status). |
| `Interfaces/IStockMovementRepository.cs`, `ISupplierRepository.cs`, `ILocationRepository.cs`, `IMaintenanceScheduleRepository.cs` | Repository contracts for the new aggregates. |
| `Interfaces/IStockService.cs`, `ISupplierService.cs`, `ILocationService.cs`, `IMaintenanceService.cs` | Application-service contracts. |
| `DTOs/StockMovementDto.cs` | `StockMovementDto` + `ReceiveStockDto`, `AdjustStockDto`, `DisposeStockDto`, `TransferStockDto`. |
| `DTOs/SupplierDto.cs`, `LocationDto.cs`, `MaintenanceScheduleDto.cs` | Read + create/update (+ `CompleteMaintenanceDto`) payloads with DataAnnotations. |

### Infrastructure (`server/InventoryManagement.Infrastructure`)
| File | Purpose |
| --- | --- |
| `Data/Configurations/StockMovementConfiguration.cs` | Table map, indexes, FKs (required item = Restrict; optional actors/links = SetNull), soft-delete filter. |
| `Data/Configurations/SupplierConfiguration.cs` | Unique-name filtered index, soft-delete filter. |
| `Data/Configurations/LocationConfiguration.cs` | Unique-code filtered index, self-FK hierarchy (Restrict), soft-delete filter. |
| `Data/Configurations/MaintenanceScheduleConfiguration.cs` | Due-date/status indexes, relationships, soft-delete filter. |
| `Repositories/StockMovementRepository.cs`, `SupplierRepository.cs`, `LocationRepository.cs`, `MaintenanceScheduleRepository.cs` | EF Core repositories. |
| `Migrations/…_AddGroup2StockSupplierLocationMaintenance.cs` | Adds `StockMovements`, `Suppliers`, `Locations`, `MaintenanceSchedules` tables + additive columns on `Inventories`/`InventoryAssignments`. Applied on startup via `MigrateAsync`. |

### Application (`server/InventoryManagement.Application`)
| File | Purpose |
| --- | --- |
| `Services/StockService.cs` | Receive/adjust/dispose/transfer + ledger reads; transactional, invariant-guarded. |
| `Services/SupplierService.cs` | CRUD with name-uniqueness and delete-guard (blocks delete while items link). |
| `Services/LocationService.cs` | CRUD with code-uniqueness, parent existence, **cycle prevention**, delete-guard. |
| `Services/MaintenanceService.cs` | CRUD + completion (recurring roll-forward) + due tracking; read-time status normalization. |

### API (`server/InventoryManagement.API`)
| File | Purpose |
| --- | --- |
| `Controllers/SuppliersController.cs`, `LocationsController.cs`, `MaintenanceController.cs` | Thin controllers mapping `Result<T>` → `ApiResponse<T>`; reads = all roles, writes = Admin/Provider, deletes = Admin. |

## 3. Modified backend files

- **`Entities/Inventory.cs`** — added `ReorderLevel`, `ReorderQuantity`, `SupplierId` (+ `SupplierEntity` nav), `LocationId` (+ `LocationEntity` nav), and `StockMovements`/`MaintenanceSchedules` collections. Legacy free-text `Supplier`/`Location` **retained** for backward compatibility (R1).
- **`Entities/InventoryAssignment.cs`** — added `ReturnedQuantity`, `RenewalCount`, `ReturnCondition?` (enables partial returns / renew / condition-on-return without breaking the existing return flow).
- **`Data/AppDbContext.cs`** — registered the four new `DbSet`s.
- **`Data/Configurations/InventoryConfiguration.cs`** — `SupplierId`/`LocationId` indexes + FK relationships (Restrict so referenced reference-data can't be hard-deleted).
- **`Data/Configurations/InventoryAssignmentConfiguration.cs`** — `ReturnedQuantity`/`RenewalCount` defaults (0) + `ReturnCondition` int conversion.
- **`Interfaces/IUnitOfWork.cs` + `Repositories/UnitOfWork.cs`** — exposed `StockMovements`, `Suppliers`, `Locations`, `MaintenanceSchedules` (now 7 repositories over the shared session).
- **`Interfaces/IInventoryRepository.cs` + `Repositories/InventoryRepository.cs`** — added `GetReorderInventoriesAsync`, `GetCountsBySupplierAsync`, `GetCountsByLocationAsync` (single grouped query, no N+1) and loaded supplier/location navs on barcode/detail reads.
- **`Interfaces/IInventoryService.cs` + `Services/InventoryService.cs`** — added `GetReorderInventoriesAsync`; main reads now include the managed supplier/location navigations so their names project into `InventoryDto`.
- **`Services/InventoryAssignmentService.cs`** — create now writes an `Assigned` ledger row; return supports **partial quantity + condition** (Lost reduces owned `Quantity`; others credit `AvailableQuantity`) and writes a `Returned`/`Disposed` ledger row; added `RenewAssignmentAsync` and `GetDueSoonAssignmentsAsync`.
- **`Controllers/InventoryController.cs`** — injected `IStockService`; added `receive`, `adjust`, `dispose`, `transfer`, `reorder`, `{id}/movements`, `movements/recent`.
- **`Controllers/InventoryAssignmentsController.cs`** — added `renew` + `due-soon`; `return` now accepts the extended DTO.
- **`Application/Mapping/MappingExtensions.cs`** — extended Inventory/Assignment maps; added Supplier/Location/StockMovement/MaintenanceSchedule maps.
- **`Application/Extensions/ServiceCollectionExtensions.cs`** — registered `StockService`, `SupplierService`, `LocationService`, `MaintenanceService`.
- **`Infrastructure/Extensions/ServiceCollectionExtensions.cs`** — registered the four new repositories.
- **`Domain/Constants/BusinessConstants.cs`** — added `Assignment.DefaultDueSoonDays` and a `Maintenance` window group.
- **`DTOs/InventoryDto.cs`** — added reorder/supplier/location fields + computed `NeedsReorder`.
- **`DTOs/InventoryAssignmentDto.cs`** — added `ReturnedQuantity`, `OutstandingQuantity`, `RenewalCount`, `ReturnCondition`; extended `ReturnInventoryAssignmentDto`; added `RenewInventoryAssignmentDto`.

## 4. New / modified frontend files (Rule 9 — design system preserved)

**New pages:** `client/src/app/(app)/suppliers/page.tsx`, `locations/page.tsx`, `maintenance/page.tsx`.
**New components:** `components/suppliers/supplier-form-modal.tsx`, `components/locations/location-form-modal.tsx`,
`components/maintenance/maintenance-form-modal.tsx`, `maintenance-complete-modal.tsx`,
`components/inventory/stock-action-modal.tsx` (unified receive/adjust/dispose/transfer),
`components/assignments/renew-modal.tsx`.

**Modified:**
- `lib/types.ts` — new enums + DTOs; extended Inventory/Assignment/Return DTOs.
- `lib/api.ts` — `inventory.{reorder,movements,recentMovements,receive,adjust,dispose,transfer}`, `assignments.{renew,dueSoon}`, new `suppliers`/`locations`/`maintenance` groups.
- `lib/utils.ts` — label/tone maps for the new enums.
- `components/domain/status-badges.tsx` — `StockMovementBadge`, `MaintenanceStatusBadge`, `ReturnConditionBadge`.
- `components/layout/nav.ts` + `sidebar.tsx` — Suppliers/Locations/Maintenance nav (manager-gated via `canManage`).
- `app/(app)/inventory/[id]/page.tsx` — stock action buttons, movements ledger card, maintenance card, reorder/supplier/location display, "Reorder" badge.
- `components/inventory/inventory-form-modal.tsx` — reorder level/qty + managed supplier/location selects (lazy-loaded, active only).
- `components/assignments/return-modal.tsx` — partial-return quantity + condition.
- `app/(app)/assignments/page.tsx` — Renew action + modal.

## 5. New tests

- `tests/…Application.Tests/StockServiceTests.cs` — receive/adjust/dispose/transfer invariants + ledger rows + not-found/validation guards (10 tests).
- `tests/…Application.Tests/MaintenanceServiceTests.cs` — due-status on create, recurring roll-forward, one-off close (3 tests).
- `tests/…Application.Tests/InventoryAssignmentServiceTests.cs` — added partial-return, lost-return (reduces owned qty), and renew tests.

## 6. Domain-agnostic guarantees

- New entities use generic vocabulary (movement / supplier / location / maintenance), never medical terms.
- Additive schema only: legacy `Supplier`/`Location` strings preserved; every new column/table is optional/nullable, so existing clients and the existing API contract keep working unchanged (R1 — no API/ABI break).
- Recipient = any person/department; item = any SKU; supplier/location/maintenance are universal inventory concepts.

## 7. Known limitations / future extensions

- **Transfers** move a whole item's location (single `Inventory` row). Per-location split stock would need a `(Inventory, Location)` stock-level join — deferred (Plan A) to avoid a breaking model change.
- **Damaged/NeedsRepair returns** credit available stock and record the condition; routing damaged units to a separate quarantine bucket is a future enhancement (users can `adjust`/`dispose` or schedule maintenance).
