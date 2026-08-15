# Group 1 Feature Changes — Utilizing existing backend capabilities

> Scope: **frontend-only** (the `client/` app). **No backend changes.** Every feature
> consumes endpoints the ASP.NET API already exposes, so the backend stays
> **domain-agnostic and any-industry reusable**. The medical framing is only the demo.

Verification: `npx tsc --noEmit` clean · `next build` clean (11 routes) · all 8 features
smoke-tested live against the running API (see "Verification" at the end).

---

## 1. Mind map — request/response flow (Rule 5)

```
Browser (same-origin)                Next dev/proxy            ASP.NET API (existing)
──────────────────────               ───────────────          ──────────────────────
BarcodeLookup ─────────► GET /api/inventory/barcode/{code} ─► InventoryController.GetByBarcode
  └─ router.push(/inventory/{id})
InventoryDetailPage ───► GET /api/inventory/{id}          ─► InventoryController.GetById
  └─ (canManage) ──────► GET /api/inventoryassignments/history/inventory/{id}
InventoryPage (list) ──► POST /api/inventory/search  (filters set)   ─► SearchInventories
                         GET  /api/inventory/paged   (no filters)    ─► GetInventoriesPaged
UserAssignmentsModal ──► GET /api/inventoryassignments/user/{userId} ─► GetAssignmentsByUserId
Dashboard controls ────► GET /api/inventory/expiring?monthsBefore=N  ─► GetExpiringInventories
                         GET /api/inventory/low-stock?threshold=N    ─► GetLowStockInventories
Notifications bell ────► GET /api/dashboard/alerts/summary           ─► GetAlertsSummary
ReportsPage ───────────► GET /api/inventory + /api/inventoryassignments (compute KPIs client-side)
```

Scale note (Rule 5): the inventory list is now **server-paginated and server-searched**
(one request per interaction, debounced), so it holds up for large catalogs instead of
fetching the whole table into the browser.

---

## 2. New files

| File | Purpose |
| --- | --- |
| [client/src/lib/use-debounce.ts](../client/src/lib/use-debounce.ts) | `useDebouncedValue` — coalesces keystrokes so search hits the server once per pause. |
| [client/src/lib/export.ts](../client/src/lib/export.ts) | First-party CSV export (`toCsv`, `exportCsv`, `downloadText`, `timestampedName`) using the Blob + Object-URL Web APIs — **no third-party dependency** (Rule 8). RFC 4180 quoting. |
| [client/src/components/layout/barcode-lookup.tsx](../client/src/components/layout/barcode-lookup.tsx) | Topbar barcode quick-lookup. Auto-focused field works with USB/handheld scanners (type + Enter) and manual entry → navigates to the item. |
| [client/src/app/(app)/inventory/[id]/page.tsx](../client/src/app/(app)/inventory/[id]/page.tsx) | Deep-linkable item detail page (full fields, stock bar, description/notes, assignment history for privileged users, Edit/Delete actions reusing existing modals). |
| [client/src/components/users/user-assignments-modal.tsx](../client/src/components/users/user-assignments-modal.tsx) | "Who has what" — every assignment held by one recipient. |
| [client/src/components/layout/notifications.tsx](../client/src/components/layout/notifications.tsx) | Topbar alerts bell (expiry + low-stock + overdue counts) via the single `alerts/summary` call. |
| [client/src/app/(app)/reports/page.tsx](../client/src/app/(app)/reports/page.tsx) | Reports: inventory value, value-by-category, status breakdown, utilization, most-assigned; CSV export + print. |

## 3. Modified files

### [client/src/components/ui/input.tsx](../client/src/components/ui/input.tsx)
- **Change:** converted `Input` to `React.forwardRef`.
- **Why:** the barcode field must auto-focus (so scanners fire immediately); refs need forwarding.
- **Compat/benefit:** fully backward-compatible (transparent to all existing `<Input>` usages); enables imperative focus without a DOM hack.

### [client/src/components/layout/app-shell.tsx](../client/src/components/layout/app-shell.tsx)
- Added `<Notifications />` and `<BarcodeLookup />` to the topbar right cluster.
- Added Tailwind `print:` variants (`print:hidden` on sidebar/topbar, `print:pl-0` on the content wrapper) so the Reports **Print** output is clean.
- **Benefit:** global barcode + alerts access; print-friendly reports with no extra CSS file.

### [client/src/components/layout/nav.ts](../client/src/components/layout/nav.ts)
- Added the **Reports** nav item (`/reports`, all roles).

### [client/src/app/(app)/users/page.tsx](../client/src/app/(app)/users/page.tsx)
- Added a **View assignments** row action + `viewAssignmentsFor` state + `<UserAssignmentsModal>`.
- **Benefit:** admins can see any recipient's ledger (previously-unused `assignments/user/{id}`).

### [client/src/app/(app)/dashboard/page.tsx](../client/src/app/(app)/dashboard/page.tsx)
- Split the fixed expiry/low-stock alert fetches out of the main dashboard load into two
  independently-refetched hooks driven by new **Window (3–6 months)** and **Threshold (≤N)** controls,
  calling `inventory/expiring?monthsBefore` and `inventory/low-stock?threshold`.
- `AlertColumn` gained an optional `control` slot.
- **Benefit:** tunable alerts without reloading the whole dashboard; demonstrates the parameterised endpoints.

### [client/src/app/(app)/inventory/page.tsx](../client/src/app/(app)/inventory/page.tsx) — largest change
- **Removed** the client-side "fetch all + filter/slice in the browser" model (`api.inventory.list()` + two `useMemo`s + client pagination).
- **Added** server-driven data:
  - filters set → `POST /api/inventory/search` (text + category + status + **expiry date range** + page), with a full-page "has next" heuristic (the endpoint returns items without a total);
  - no filters → `GET /api/inventory/paged` (returns an accurate total → "Page N · M items total").
- Search text is **debounced** (`useDebouncedValue`), an **Advanced filters** panel adds category + expiry-from/to, item names are now **deep-links** to the detail page, and pagination is a server-driven Prev/Next.
- **Why / benefit (Rule 5):** scales to large catalogs (no whole-table download); demonstrates the server search + date-range + pagination the backend was built for. Old data is retained during refetch (via the `useAsync` guard) so paging/searching doesn't flicker.

---

## 4. Backend endpoints newly utilized (previously wired but unused)

`inventory/barcode/{code}` · `inventory/{id}` · `inventory/search` · `inventory/paged` ·
`inventory/expiring?monthsBefore` · `inventory/low-stock?threshold` ·
`inventoryassignments/user/{userId}` · `inventoryassignments/history/inventory/{id}` (now on the detail page too) ·
`dashboard/alerts/summary`.

## 5. Verification

- **Static:** `npx tsc --noEmit` → 0 errors. `next build` → success, 11 routes (`/inventory/[id]` dynamic, `/reports` added).
- **Live (Playwright, real API):**
  - Inventory pager `Page 1 · 5 items total` (`/paged`); search "thermometer" → 1 row `Digital Thermometer`, pager `Page 1` (`/search`); clear → 5 rows.
  - Barcode `1234567890123` → `/inventory/1` detail page with full fields + history.
  - Reports: `Total stock value $5,038.98`, value-by-category, `Utilization 4%`, most-assigned; CSV + Print buttons.
  - Notifications bell: `4` (Expiring 2, Low stock 2, Overdue 0) via `alerts/summary`.
  - Dashboard: Window + Threshold controls present.
  - Who-has-what: Dr. Sarah Johnson → 1 active / 2 total.

## 6. Not in this change (kept generic / future)
Group 2 ideas (restock/reorder, stock-movement ledger, locations/transfers, suppliers directory,
assignment lifecycle extras, maintenance schedules) would need small **domain-agnostic** backend
additions and are intentionally out of scope here.
