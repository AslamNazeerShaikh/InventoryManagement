# UI Testing Guide — Inventory Management System (MediStock client)

## Overview

This guide is the front-end counterpart to the [API Testing Guide](API-Testing-Guide.md). It documents **every screen, operation, permission combination and user journey** in the **MediStock** web client (`client/`) — a Next.js + React + TypeScript + Tailwind app that consumes the ASP.NET Core API.

Use this guide to:

- Understand what the UI can do and how each feature works.
- Exercise every operation as **Admin**, **Provider (Nurse Practitioner)** and **Staff**.
- Verify role-based access, validation, error handling, animations and responsive behavior.
- Run structured, repeatable manual test passes (permutations of *role × page × action*).

> The client never talks to the API directly from the browser. The Next dev server proxies `/api/*` to the API (same-origin), so there are **no CORS or certificate prompts**. See [`client/next.config.ts`](../client/next.config.ts).

---

## 1. Prerequisites & running the app

### 1.1 Start the API (HTTP profile)

```powershell
cd "c:\Users\aslams\source\repos\localDev\InventoryManagement"
dotnet run --project server/InventoryManagement.API --launch-profile http
```

- API listens on **http://localhost:5050**.
- The `http` profile is intentional: it keeps the HTTPS redirect a no-op so the Next proxy can reach the API over plain HTTP. (A `Failed to determine the https port for redirect.` warning in the log is expected and harmless.)

### 1.2 Start the client

```powershell
cd "c:\Users\aslams\source\repos\localDev\InventoryManagement\client"
npm install    # first time only
npm run dev
```

- Client runs on **http://localhost:3000**.
- Open http://localhost:3000 — you are redirected to `/login` when signed out, or to `/dashboard` when a session exists.

### 1.3 Health signal

The top bar shows an **API status pill** that polls `/api/health` every 30 seconds:

- Green pulsing dot + "API online" — proxy and API are reachable.
- Grey "Checking API" — first poll in flight.
- Red "API offline" — API is down or unreachable.

**Test:** stop the API → within ~30 s the pill turns red; restart it → it returns to green.

---

## 2. Test accounts

The database is seeded with an administrator on first run. The demo data script (used during setup) also adds a provider and a staff member. Use these to test each role:

| Role | Email | Password | Flags |
| --- | --- | --- | --- |
| **Admin** | `admin@inventorymanagement.com` | `ChangeMe_LocalDev!2026` | `isAdmin` |
| **Provider** (Nurse Practitioner) | `sarah.johnson@hospital.com` | `Nurse@12345` | `isProvider` |
| **Staff** | `john.smith@hospital.com` | `Staff@12345` | — |

> If the provider/staff accounts do not exist yet, sign in as Admin and create them from **Users → Invite user** (see §10.2). To fully reset, stop the API and delete `server/InventoryManagement.Infrastructure/inventory.db`, then restart (a fresh admin is re-seeded).

---

## 3. Roles, flags & the permission model

Access is driven by two boolean flags on the signed-in user, **not** by the role label alone:

- **`isAdmin`** → full access, including user management and inventory deletion.
- **`isProvider`** → may manage inventory and assignments.
- Neither flag (Staff) → read-only access to their own data.

The client derives:

- `canManage = isAdmin || isProvider` (manage inventory & assignments)
- `canManageUsers = isAdmin`

These mirror the API authorization policies `AdminOnly`, `AdminOrProvider`, and `AllRoles`.

### 3.1 Permission matrix

| Capability | Admin | Provider | Staff |
| --- | :---: | :---: | :---: |
| Sidebar shows **Users** item | ✓ | ✗ | ✗ |
| Dashboard: stats, distribution, expiry & low-stock alerts, recent inventory | ✓ | ✓ | ✓ |
| Dashboard: **Overdue** alerts + **Recent assignments** | ✓ | ✓ | ✗ |
| Inventory: list / search / filter / view details | ✓ | ✓ | ✓ |
| Inventory: assignment **history** inside details | ✓ | ✓ | ✗ |
| Inventory: **Add** / **Edit** | ✓ | ✓ | ✗ |
| Inventory: **Delete** | ✓ | ✗ | ✗ |
| Assignments: view **all** | ✓ | ✓ | ✗ |
| Assignments: view **own** (my assignments) | ✓ | ✓ | ✓ |
| Assignments: **Create** / **Edit** / **Return** | ✓ | ✓ | ✗ |
| Users: list / search / filter | ✓ | ✗ | ✗ |
| Users: **Invite** / **Edit** / **Delete** | ✓ | ✗ | ✗ |
| Users: delete **self** | ✗ (blocked) | — | — |
| Profile: edit own details / change password | ✓ | ✓ | ✓ |

> Staff who navigate directly to a restricted URL (e.g. `/users`) are not blocked by the router, but the API returns **403** and the page shows a friendly error state with a **Try again** button. The **Users** nav item is hidden for them regardless.

---

## 4. Global UI concepts (test these everywhere)

| Concept | What to verify |
| --- | --- |
| **Layout** | Fixed sidebar on desktop; on narrow screens a hamburger opens a slide-over drawer. |
| **Active nav** | The current section has a highlighted pill that slides smoothly between items. |
| **User menu** | Top-right avatar opens a menu with name, email, role badge, **Profile & security** link and **Sign out**. |
| **Toasts** | Every create/update/delete/return shows a success or error toast (top-right); errors include the server message. |
| **Modals** | Open with a spring animation; close via the ✕, the **Cancel** button, clicking the backdrop, or pressing **Esc**; background scroll is locked. |
| **Search** | Client-side, instant, case-insensitive; resets to page 1 on each change. |
| **Filters** | Dropdowns combine with search (AND logic). |
| **Pagination** | "Showing *x–y* of *n*"; Prev/Next disable at the ends (8 rows/page). |
| **Loading** | Shimmer skeleton rows/cards while data loads. |
| **Empty state** | Friendly icon + message when a list is empty or no rows match filters. |
| **Error state** | Icon + message + **Try again** (re-runs the request) when a request fails. |
| **Animated numbers** | Dashboard stat values count up on load. |
| **Reduced motion** | With OS "reduce motion" enabled, animations are minimized. |

---

## 5. Authentication

### 5.1 Sign in

1. Go to `/login`.
2. Click **Use demo admin** to prefill the seeded admin credentials (or type any account's email/password).
3. Toggle the eye icon to show/hide the password.
4. Click **Sign in**.

**Expected:** button shows a "Signing in" spinner → success toast "Welcome back" → redirect to `/dashboard`.

**Negative cases:**

| Input | Expected |
| --- | --- |
| Wrong password | 401 → inline red error + toast "Sign in failed / Invalid email or password". |
| Malformed email | Native email validation blocks submit. |
| Empty fields | Native "required" validation blocks submit. |
| > 10 attempts in 60 s | 429 → toast "Too many attempts. Please wait a moment and try again." (auth rate limit). |

### 5.2 Session persistence & auto-refresh

- Tokens are stored in `localStorage`. Reloading the page keeps you signed in.
- When the access token expires, the next API call transparently uses the refresh token to get a new one and retries — **no interruption**.

**Test:** sign in, wait/refresh the page, keep navigating — you stay authenticated.

### 5.3 Session expiry

- If the refresh token is invalid/expired (e.g. after a password change, or DB reset), the next protected call fails → toast **"Session expired"** → redirect to `/login`.

### 5.4 Sign out

- From the **sidebar footer** logout icon, or the **user menu → Sign out**.
- **Expected:** the refresh token is revoked server-side, local session is cleared, redirect to `/login`.

---

## 6. Dashboard (`/dashboard`)

Landing page for every role. Sections:

1. **Greeting header** — "Welcome back, *FirstName*" and today's date.
2. **Stat cards (8)** — Total items, Available, Assigned, Team members, Expiring soon, Low stock, Active assignments, Overdue. Values animate on load.
3. **Inventory distribution** — animated donut (Available / Assigned / Other) with a legend.
4. **Attention columns** — Expiring and Low stock for everyone; **Overdue** appears only for Admin/Provider.
5. **Recent inventory** — last items added (View all → Inventory).
6. **Recent assignments** — Admin/Provider only (View all → Assignments).

**Steps to test:**

1. Sign in as **Admin** → confirm all 8 stats, the donut, all three attention columns, and both recent lists render.
2. Sign in as **Staff** → confirm the **Overdue** column and **Recent assignments** card are **absent**, and the greeting uses the staff first name.
3. Cross-check numbers against the API (`/api/dashboard/stats`) or the Inventory/Assignments pages.
4. Stop the API → the health pill goes red and lists show the error state with **Try again**.

---

## 7. Inventory (`/inventory`)

### 7.1 Browse, search & filter (all roles)

1. Open **Inventory**. Columns: Item (name + category · brand), Barcode, Stock (available/total with a bar), Status badge, Expiry, Actions.
2. **Search** by name, barcode, serial, brand, category or model — results filter instantly.
3. **Category** filter — pick a category; combine with search.
4. **Status** filter — Available / Assigned / Reserved / Expired / Damaged / Disposed.
5. **Expiry coloring:** dates within ~90 days show amber; past dates show red.
6. **Pagination** — 8 rows/page.
7. **Refresh** icon re-fetches the list (spins while loading).

### 7.2 View details (all roles)

1. Click the **eye** icon on a row.
2. A modal shows all fields, a stock bar, description and notes.
3. **Admin/Provider only:** an **Assignment history** section lists every allocation for that item with status badges; Staff do not see this section.

### 7.3 Create (Admin / Provider)

1. Click **Add item**.
2. Required: **Equipment name**. Optional: category, supplier, brand, model, serial, barcode, quantity (≥ 1), purchase price (≥ 0), manufacture/expiry dates, location, description, notes.
3. Click **Add item**.

**Expected:** toast "Inventory added" → modal closes → the item appears in the list (barcode, stock e.g. `1 / 1`, status Available).

### 7.4 Edit (Admin / Provider)

1. Click the **pencil** icon on a row.
2. The form is pre-filled; the edit form additionally exposes **Status** and allows quantity ≥ 0.
3. Change a field → **Save changes**.

**Expected:** toast "Inventory updated" → the row reflects the change.

### 7.5 Delete (Admin only)

1. As Admin, click the **trash** icon.
2. Confirm in the dialog.

**Expected:** toast "Item deleted" → row removed.
**Provider/Staff:** the trash icon is not shown.

### 7.6 Validation & error cases

| Case | Expected |
| --- | --- |
| Empty equipment name | Toast "Equipment name is required". |
| Quantity < 1 on create | Toast "Quantity must be at least 1". |
| Duplicate barcode/serial | 409 → toast with the server conflict message. |
| Negative price | Field constraint / server validation → 400 message. |

---

## 8. Assignments (`/inventoryassignments` → `/assignments`)

Behavior differs by role:

- **Admin/Provider:** see **all** assignments, may **create/edit/return**, and can filter by **Overdue**.
- **Staff:** see **only their own** assignments (read-only); header reads "Equipment currently assigned to you."; no create/edit/return; no Assignee column.

### 8.1 Browse & filter

1. Columns (Admin/Provider): Item, Assignee (avatar/name/email), Qty, Assigned, Expected return, Status, Actions.
2. **Search** by item or assignee.
3. **Status** filter includes a special **Overdue** option (Active + past expected-return date). Overdue rows show the expected-return date in red with "(*n*d late)".
4. Pagination — 8 rows/page.

### 8.2 Create (Admin / Provider)

1. Click **New assignment**.
2. **Equipment** dropdown lists items with available stock (shows quantity available); **Assignee** dropdown lists active users.
3. Set **Quantity** (≥ 1, ≤ available — the max hint is shown), optional **Expected return date** and **Notes**.
4. Click **Assign**.

**Expected:** toast "Assignment created" → the item's available stock decreases by the assigned quantity (verify on Inventory).

### 8.3 Edit (Admin / Provider — active assignments)

1. On an **Active** row, click the **pencil**.
2. Adjust quantity, expected return date, **Status**, or notes → **Save changes**.

### 8.4 Return (Admin / Provider — active assignments)

1. On an **Active** row, click **Return**.
2. Add optional return notes → **Confirm return**.

**Expected:** toast "Return processed / *item* returned to stock" → the row's status becomes **Returned**, actions disappear, and the item's available stock is **restored** (verify on Inventory). This is a transactional operation with optimistic concurrency (concurrent conflicts return 409).

### 8.5 Staff view

1. Sign in as **Staff** → open **Assignments**.
2. Confirm: description "Equipment currently assigned to you.", **no** New assignment button, **no** Assignee column, and only the staff member's own assignments (empty state if none).

---

## 9. Users (`/users`) — Admin only

### 9.1 Browse, search & filter

1. Sign in as **Admin** → open **Users** (hidden for other roles).
2. Columns: User (avatar/name/email), Role, Status (Active/Inactive), Last login, Joined, Actions.
3. **Search** by name/email; filter by **Role** and **Active/Inactive**.
4. Your own row is tagged **"You"**, and its **Delete** is disabled ("You cannot delete yourself").

### 9.2 Invite / create

1. Click **Invite user**.
2. Fill **Full name**, **Email**, **Password** (min 8). Choose a **Role**. Toggle **Administrator** and/or **Clinical provider** as needed.
3. Click **Create user**.

**Expected:** toast "User created" → the user appears in the list.

### 9.3 Edit

1. Click the **pencil** on a row.
2. Change name/email/role, toggle **Administrator / Provider / Active** (no password field here) → **Save changes**.

**Expected:** toast "User updated"; toggling **Active** off shows an Inactive badge.

### 9.4 Delete

1. Click the **trash** on another user's row → confirm.
2. **Expected:** toast "User deleted" → row removed.
3. Your own delete button is disabled (see 9.1).

### 9.5 Validation

| Case | Expected |
| --- | --- |
| Missing name/email | Toast "Name and email are required". |
| Password < 8 (create) | Toast "Password must be at least 8 characters". |
| Duplicate email | 409 → toast with the server message. |

---

## 10. Profile & security (`/profile`) — all roles

1. **Personal details** — edit **Full name** and **Email** → **Save changes** (updates your own profile; a non-admin cannot change their own role/flags). Toast "Profile updated"; the name updates in the sidebar and user menu.
2. **Change password** — enter current, new (min 8), and confirm (must match) → **Update password**. Toast "Password changed / Other sessions have been signed out."
   - Mismatch → toast "New password and confirmation do not match".
   - Wrong current password → 400/401 → toast with the server message.
   - After changing, other sessions' refresh tokens are revoked; those sessions drop to `/login` on their next call.
3. **Account summary** (right card) — avatar, name, email, role badge, Admin/Provider badges, Active status, Account ID, "Member since", and "Last login".

---

## 11. Role-based journeys (user permutations)

### 11.1 As **Admin** (full control)

- All nav items visible.
- Dashboard: everything (incl. Overdue + Recent assignments).
- Inventory: view/create/edit/**delete**, details **with** history.
- Assignments: view all, create/edit/return.
- Users: full CRUD (except self-delete).
- Profile: edit details + password.

### 11.2 As **Provider** (Nurse Practitioner, `isProvider`)

- Nav: Dashboard, Inventory, Assignments, Profile (**no Users**).
- Dashboard: full incl. Overdue + Recent assignments.
- Inventory: view/create/edit, details **with** history; **cannot delete**.
- Assignments: view all, create/edit/return.
- Users: not accessible (nav hidden; direct URL → 403 error state).
- Profile: edit details + password.

### 11.3 As **Staff**

- Nav: Dashboard, Inventory, Assignments, Profile (**no Users**).
- Dashboard: no Overdue column, no Recent assignments.
- Inventory: view/search/filter/details only (**no** history, **no** add/edit/delete).
- Assignments: **only their own**, read-only.
- Profile: edit details + password.

### 11.4 End-to-end multi-user scenario

1. **Admin** signs in → **Users → Invite user**: create a Provider (Provider toggle on) and a Staff member.
2. **Admin** → **Inventory → Add item**: add a piece of equipment with stock.
3. Sign out → sign in as the **Provider**.
4. **Provider** → **Assignments → New assignment**: assign the item to the **Staff** member (stock decreases).
5. Sign out → sign in as the **Staff** member → **Assignments**: confirm the item appears under "my assignments" (read-only).
6. Sign out → sign in as **Provider** → **Assignments → Return**: process the return (stock restored; status Returned).
7. Sign in as **Admin** → **Dashboard**: verify Active/Overdue counts and distribution reflect the changes.

---

## 12. Combinatorial action matrix (page × action × role)

Legend: ✓ available/allowed · ✗ hidden/blocked · **own** = only their own records.

| Page / Action | Admin | Provider | Staff |
| --- | :---: | :---: | :---: |
| Dashboard — view core | ✓ | ✓ | ✓ |
| Dashboard — Overdue + Recent assignments | ✓ | ✓ | ✗ |
| Inventory — list/search/filter/details | ✓ | ✓ | ✓ |
| Inventory — details history | ✓ | ✓ | ✗ |
| Inventory — create | ✓ | ✓ | ✗ |
| Inventory — edit | ✓ | ✓ | ✗ |
| Inventory — delete | ✓ | ✗ | ✗ |
| Assignments — view | ✓ (all) | ✓ (all) | ✓ (**own**) |
| Assignments — create | ✓ | ✓ | ✗ |
| Assignments — edit (active) | ✓ | ✓ | ✗ |
| Assignments — return (active) | ✓ | ✓ | ✗ |
| Users — list/search/filter | ✓ | ✗ | ✗ |
| Users — invite/edit/delete | ✓ | ✗ | ✗ |
| Users — delete self | ✗ | — | — |
| Profile — edit details/password | ✓ | ✓ | ✓ |

---

## 13. Error & status handling to verify

| HTTP | When | UI behavior |
| --- | --- | --- |
| **400** | Client/DTO validation failure | Toast with the validation message (joined details). |
| **401** | Expired/invalid access token | Silent refresh + retry; if refresh fails → "Session expired" → `/login`. |
| **403** | Role lacks permission (e.g. Staff → `/users`) | Error state with **Try again**; restricted nav items are hidden. |
| **404** | Missing record | Toast / error state with the not-found message. |
| **409** | Duplicate (barcode/serial/email) or concurrency conflict | Toast with the conflict message. |
| **429** | Auth rate limit (login/refresh) | Toast "Too many attempts…". |
| **Network** | API down | "Cannot reach the API…" toast; health pill red. |

---

## 14. Responsiveness & accessibility

- **Mobile (< 1024px):** the sidebar collapses to a hamburger → slide-over drawer; tables hide secondary columns (Barcode, Expiry, Last login, Joined) and scroll horizontally.
- **Keyboard:** modals close on **Esc**; inputs are focusable; the password field has a show/hide toggle with an aria-label.
- **Reduced motion:** OS "reduce motion" minimizes animations.
- **Contrast:** dark theme with accessible status colors (success/warning/danger/info).

---

## 15. Test data reset & troubleshooting

| Symptom | Fix |
| --- | --- |
| Lists empty / want a clean slate | Stop the API, delete `server/InventoryManagement.Infrastructure/inventory.db`, restart (admin re-seeded). |
| Health pill red | Ensure the API runs on **http://localhost:5050** with the **http** profile. |
| `/login` loops or "Session expired" | Clear site data for `localhost:3000`, or the refresh token was revoked (e.g. after a password change) — sign in again. |
| Changed API port | Set `API_PROXY_TARGET` before `npm run dev`, or edit [`client/next.config.ts`](../client/next.config.ts). |
| 401 on every call after DB reset | Sign out and sign back in to obtain fresh tokens. |

---

## 16. Quick regression checklist

- [ ] Login (demo admin) → Dashboard renders with animated stats.
- [ ] Health pill green; turns red when API stops.
- [ ] Inventory: search, category & status filters, pagination.
- [ ] Inventory: create → edit → view details (history) → delete (Admin).
- [ ] Assignments: create (stock ↓) → return (stock ↑, status Returned); Overdue filter.
- [ ] Users (Admin): invite → edit (toggle Active) → delete; self-delete blocked.
- [ ] Profile: edit name/email; change password (mismatch rejected).
- [ ] Sign in as Provider → no Users nav; cannot delete inventory.
- [ ] Sign in as Staff → no Users nav, own assignments only, no create buttons, dashboard hides Overdue/Recent assignments.
- [ ] Sign out revokes session and returns to `/login`.

---

_This document is maintained alongside the client. Update it when UI features, routes, or permissions change._
