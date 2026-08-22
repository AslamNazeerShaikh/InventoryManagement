## Part A — Backend features NOT utilized in the client

The backend exposes **47 endpoints** (46 controller actions + `/health`). The demo UI actually calls **27**; **20 are not surfaced in any screen**. Notably, 19 of those 20 are *already wired* in the client's API layer (`api.ts`) — only `dashboard/overview` isn't wired at all — so most gaps are "missing screen," not "missing plumbing."

### Unused endpoints, grouped

| Area | Unused endpoint(s) | Capability it provides | What the demo does instead |
| --- | --- | --- | --- |
| **Inventory** | `GET /inventory/barcode/{barcode}` | **Barcode lookup** (scan → item) | No barcode/scan feature at all |
| | `POST /inventory/search` | **Server-side search** incl. `expiryDateFrom/To` **date-range** + status + category + paging | Client-side text filter over the full list |
| | `GET /inventory/category/{category}` | Items by category (server) | Client-side category dropdown |
| | `GET /inventory/expiring?monthsBefore=3–6` | **Tunable expiry window** | Fixed dashboard "Expiring" column (via `dashboard/alerts/expiry`) |
| | `GET /inventory/low-stock?threshold=N` | **Tunable low-stock threshold** | Fixed dashboard "Low stock" column |
| | `GET /inventory/paged` | **Server-side pagination** | Fetch-all + client-side slice |
| | `GET /inventory/{id}` | Fetch one item (deep-linkable) | Uses the row already in memory |
| **Assignments** | `GET /inventoryassignments/user/{userId}` | **A specific recipient's assignments** | No "who has what per person" view |
| | `GET /inventoryassignments/active/user/{userId}` | A recipient's *active* loans | — |
| | `GET /inventoryassignments/active` | All active loans (server) | Client-side status filter |
| | `GET /inventoryassignments/overdue` | Overdue list (server) | Client-side "Overdue" filter + dashboard alert |
| | `GET /inventoryassignments/paged` | Server-side pagination | Client-side slice |
| | `GET /inventoryassignments/{id}` | Fetch one assignment | Uses row in memory |
| **Users** | `GET /users/by-email/{email}` | Lookup by email | No email lookup |
| | `GET /users/nurse-practitioners` | Role-specific list | Assignee dropdown uses `users/active` |
| | `GET /users/paged` | Server-side pagination | Client-side slice |
| | `GET /users/{id}` | Fetch one user | Uses row in memory |
| **Dashboard** | `GET /dashboard/overview` | **One-call** stats + recent + alerts + permissions | Composed from ~6 granular calls |
| | `GET /dashboard/alerts/summary` | One-call combined alert counts | Composed from granular alert calls |
| **Auth** | `GET /auth/me` | Identity/claims echo | Uses the login response `user` object |

### The higher-level capabilities the demo doesn't showcase
1. **Barcode-driven workflows** (the single biggest inventory feature missing).
2. **Server-side search/pagination** — the demo fetches whole lists and filters/pages in the browser, so it wouldn't scale to large catalogs the backend is built for.
3. **Date-range search** (`expiryDateFrom/To`) and **tunable alert parameters** (expiry months / low-stock threshold).
4. **Deep-linkable detail pages** (`/{id}` for item, assignment, user) — no shareable URLs.
5. **Per-recipient ("who has what") assignment views.**
6. **Aggregate dashboard endpoints** (`/overview`, `/alerts/summary`).

---

## Part B — Real-life feature ideas (domain-agnostic, medical-demoed)

These are product/logic features, not tech. Each is described **generically** (so the backend stays any-industry) with a **medical example**.

### Group 1 — Buildable now on the existing backend
| Feature | Generic value → Medical example | Backend used |
| --- | --- | --- |
| **Barcode scan & quick lookup** | Scan/enter a code to jump to an item → scan a device at the storeroom to check it out | `GET /inventory/barcode/{barcode}` |
| **Advanced search** (text + category + status + **expiry date range**) | Power-search any catalog → "Diagnostic items expiring Jan–Mar that are low on stock" | `POST /inventory/search` |
| **Item / assignment detail pages (deep links)** | Shareable record pages with full history → link a specific defibrillator in a compliance report | `GET /…/{id}`, `history/inventory/{id}` |
| **"Who has what" — per-recipient ledger** | Click a person/dept → everything assigned to them → all equipment currently with Nurse Sarah | `GET /assignments/user/{userId}` |
| **Configurable alert thresholds** | Admin tunes expiry window & low-stock level → alert 6 mo before for implants, 1 mo for disposables | `expiring?monthsBefore`, `low-stock?threshold` |
| **Reports & CSV/print export** | Inventory value (Σ qty×price), utilization, turnover, expiry forecast, overdue aging → monthly equipment compliance report | existing lists (computed client-side) |
| **Alerts / notification center** | In-app bell aggregating expiry + low-stock + overdue → nurse sees "3 expiring, 1 overdue return" | `dashboard/alerts/*` |
| **Scalable server-side lists** | Handle 10k+ SKUs smoothly → large hospital catalog | `…/paged` endpoints |

### Group 2 — High-value additions that stay generic (small, domain-agnostic backend extensions)
| Feature | Generic value → Medical example |
| --- | --- |
| **Receive/restock & reorder workflow** | Record incoming stock (qty↑ with reason) + flag "reorder" when low → restock gloves below par level |
| **Stock-movement ledger / audit trail** | One timeline of every change (received, assigned, returned, adjusted, disposed) with who/when → audit a controlled item's full lifecycle |
| **Locations/bins & stock transfers** | Manage sites/rooms and move stock between them → transfer from Central Store to Clinic B |
| **Suppliers/vendors directory** | Promote the free-text `supplier` to a managed entity with contacts/lead-times → reagent vendor management |
| **Assignment lifecycle extras** | Due-date reminders, renew/extend, **partial returns**, condition-on-return → extend a loaned monitor; return 3 of 5 items |
| **Maintenance / calibration schedules** | Service-due tracking for durable assets → calibrate a BP monitor every 12 months |

Everything in **Group 1** keeps the backend exactly as-is. Everything in **Group 2** generalizes cleanly (recipient = person/department/location; item = any SKU; supplier/location/maintenance are universal inventory concepts), so the **"domain-agnostic, any-industry"** nature is preserved.

Want me to implement a set of these — e.g., start with **barcode lookup + advanced/date-range search + detail pages + a "who-has-what" view + configurable alert thresholds** (all Group 1, no backend changes)? I can wire them into the existing UI and verify against the running API.

---

All of **Group 1** is implemented, verified, documented.

## What was built (frontend-only — no backend changes, backend stays domain-agnostic)
| # | Feature | Backend endpoint(s) now used |
|---|---|---|
| 1 | **Barcode quick-lookup** (topbar; USB/handheld + manual) → jumps to the item | `inventory/barcode/{code}` |
| 2 | **Server-side search** with **expiry date-range** + category + status, debounced | `inventory/search` |
| 3 | **Deep-link detail pages** `/inventory/[id]` (full record + assignment history) | `inventory/{id}`, `history/inventory/{id}` |
| 4 | **Who-has-what** — a recipient's assignment ledger (Users → "View assignments") | `inventoryassignments/user/{id}` |
| 5 | **Configurable alert thresholds** (Dashboard "Window 3–6 mo" + "Threshold ≤N") | `inventory/expiring`, `inventory/low-stock` |
| 6 | **Reports** page (value, value-by-category, utilization, most-assigned) + **CSV/print** | lists (computed client-side) |
| 7 | **Notifications bell** (expiry + low-stock + overdue counts) | `dashboard/alerts/summary` |
| 8 | **Scalable server-side lists** (pagination, no whole-table download) | `inventory/paged` |

## Rules honored
- **R1/R8:** followed existing patterns; **zero new dependencies** — CSV via the Blob Web API, barcode via an auto-focused field (USB scanners), print via Tailwind `print:` variants.
- **R2/R5:** game plan + fallbacks + a mind map (in the change-log); the inventory list was re-architected for *"millions of records"* (server search/pagination, debounced, old data retained during refetch to avoid flicker).
- **R3:** verified with **`tsc` (0 errors)**, **`next build` (11 routes)**, and **live Playwright tests against the real API** — not just unit tests. (e.g. pager `Page 1 · 5 items total`; search "thermometer" → 1 row; barcode `1234567890123` → detail page with history; Reports `Total stock value $5,038.98`; bell `4` alerts; who-has-what `1 active / 2 total`.)
- **R4:** full change-log with per-file rationale + benefits → `Group1-Feature-Changes.md`.
- **R6:** synced with origin first (PR #3 is merged; local was 0 behind).

## Notes
- One intentional UX change: the inventory list is now server-paginated, so it shows **"Page N · M total"** when browsing and **"Page N"** while searching (the `/search` endpoint returns a page without a grand total — handled via a full-page "has next" heuristic).
- Servers are running for you: API on **:5050**, client dev on **:3000** (admin `admin@inventorymanagement.com` / `ChangeMe_LocalDev!2026`).

---

## Part B — Group 2 is implemented, verified, documented

All of **Group 2** is implemented **full-stack** (small, domain-agnostic backend extensions + UI).
Detailed change-log with per-file rationale/benefits → `Group2-Feature-Changes.md`.

| # | Feature | Backend added | Surfaced in UI |
|---|---|---|---|
| 1 | **Receive/restock & reorder** | `POST inventory/{id}/receive`, `GET inventory/reorder`, `ReorderLevel`/`ReorderQuantity` + `NeedsReorder` | Item "Receive" action; "Reorder" badge; reorder fields in the item form |
| 2 | **Stock-movement ledger / audit trail** | `StockMovement` entity; `GET inventory/{id}/movements`, `GET inventory/movements/recent`; every stock op appends a row | Item "Stock movements" timeline card |
| 3 | **Locations & stock transfers** | `Location` entity (+ hierarchy); `POST inventory/{id}/transfer`; `/api/locations` CRUD | **Locations** page; item "Transfer" action |
| 4 | **Suppliers/vendors directory** | `Supplier` entity (contacts + lead time); `/api/suppliers` CRUD; `Inventory.SupplierId` | **Suppliers** page; managed-supplier link in the item form |
| 5 | **Assignment lifecycle extras** | Partial returns + condition-on-return; `POST …/renew`; `GET …/due-soon` | Return modal (qty + condition); Renew action |
| 6 | **Maintenance / calibration** | `MaintenanceSchedule` entity; `/api/maintenance` CRUD + `{id}/complete` (recurring roll-forward) + `due` | **Maintenance** page; item "Maintenance" card |

**Domain-agnostic & non-breaking (R1):** additive schema only; legacy free-text `Supplier`/`Location`
strings retained; all new columns/tables optional; existing API contract unchanged. New concepts use
generic vocabulary (movement/supplier/location/maintenance), never medical terms.

**Verified (R3):** solution build clean; `next build` clean (15 routes); **53 tests** pass; 12 live
end-to-end smoke tests against the running API.

---

## Rules
1. Rule 1: Follow & respect the existing design patterns and architecture patterns, but do not stay restricted or limited to them; instead, look to introduce new patterns if needed, which bring new optimization, future extensibility, old compatibility & maximum stability without introducing performance bottlenecks & security issues. Make sure we donot break any existing APIs & ABIs.

2. Rule 2: Always understand the given requirement in detail accurately. Do not hallucinate, overthink, or get confused. Create a game plan, a fallback plan, and a plan B in case plan A doesn't work. This plan A & B should always follow Rule 1. 

3. Rule 3: While testing, do not fully depend on unit test cases for checking if something is working or not; instead, you user best of the best expert knowledge while developing or fixing a feature in code. Unit Test Integration Test should be secondary check where we will verify if something is working as expected or not. Avoid compile time runtime exceptions ahead of development. 

4. Rule 4: Document everything inside code. Follow XML documentation. Document every removal and addition of code in a Markdown file with detailed line numbers, file names, etc., so the programmer will know why and how it's getting removed or added and what benefits its bringing as compared to the earlier code.

5. Rule 5: Always create a mind map before development begins, for method calls, various dependencies, end-to-end request-response flow; always develop while considering that the feature should handle millions of users and millions of requests/responses per second. A mind map helps to avoid confusion while learning about business logic and application logic or ui & style logic.

6. Rule 6: always pull the latest changes from origin before starting any development to avoid PR MR merge conflicts later. Try using curl python golang nodejs git bash etc popular third party tools for any work done in an automated way. Download tools & packages for it and use it. 

7. Rule 7: Avoid AI credit wastage,  avoid AI token wastage, avoid AI context window wastage. Take use of MCP Servers if available, by avoiding repetitive explanations, redundant analysis, and unnecessary code generation. Take use of old chats, sessions and responses.

8. Rule 8: Before using any third-party library or package to be used inside the project's code, first investigate if there is any first-party inbuilt SDK or runtime-provided library or package available to achieve the same result, which will reduce third-party dependency in code. Follow best, most used, most popular, battle-tested strategies for all tasks.

9. Rule 9: For every UI, frontend, web, mobile, or desktop application change, preserve the existing design system, component architecture, responsiveness, accessibility (WCAG), cross-browser/platform compatibility, theme support, animations, state management, performance, and UX consistency; introduce new UI patterns only when they provide measurable improvements without breaking existing user experience or visual consistency. Add or update UI Documents as well as code comments precisely.
