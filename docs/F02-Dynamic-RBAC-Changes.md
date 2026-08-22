# F‑02 (Dynamic RBAC + Domain‑Agnostic Vocabulary) — Change Log

> **Status:** Stage 2 of the requirement‑fit remediation. Builds on Stage 1 (multi‑tenancy + provider‑agnostic indexes).
> **Scope:** Replace the fixed `IsAdmin`/`IsProvider`/`UserRole` model with **dynamic, per‑tenant Roles + Permissions** (client‑managed), permission‑based authorization, and neutral vocabulary (`equipmentName` → `name`). Frontend updated in lockstep. Old schema/data intentionally discarded; all EF migrations collapsed into a single clean `InitialCreate`.
> **Result:** backend build + **53/53 tests** green; runtime‑verified (login carries 16 permission claims, dynamic `/api/roles` works, renamed `name` field round‑trips); frontend `tsc --noEmit` clean.

---

## 1. Mind map — permission‑based authorization flow (Rule 5)

```mermaid
flowchart TD
    subgraph Seeding (per tenant)
      CAT[Permissions catalog code] --> SEEDP[Seed Permission rows]
      SEEDP --> SEEDR[Seed system Roles: Administrator/Provider/Staff]
      SEEDR --> GRANT[Seed RolePermission grants]
      GRANT --> ADMINROLE[Admin user -> Administrator role]
    end

    L[Login] --> RESOLVE[AuthService: set tenant from user,\nUserRoleRepo.GetRolesWithPermissionsForUser]
    RESOLVE --> TOK[TokenService: emit ClaimTypes.Role per role +\n'permission' claim per code]
    TOK --> JWT[JWT: tenant + roles + permissions]
    JWT --> C[Client stores user.roles + user.permissions]

    C -->|Bearer| REQ[Request to /api/...]
    REQ --> ATTR["[HasPermission(perm)] -> policy 'perm:...'"]
    ATTR --> PROV[PermissionPolicyProvider builds policy on demand]
    PROV --> HANDLER[PermissionAuthorizationHandler:\nUser.HasClaim(permission, code)?]
    HANDLER -->|yes| OK[200]
    HANDLER -->|no| DENY[403]

    subgraph Client-managed RBAC
      RC[RolesController /api/roles] --> RS[RoleService]
      RS --> RREPO[Role/Permission/UserRole repos]
    end
    C -.roles.manage.-> RC
```

**Authorization is stateless on the request path:** permissions are resolved once at login and carried as JWT claims; the handler is a pure claim check (no DB hit). Tenants create roles and grant permissions dynamically via `/api/roles`; users are assigned roles via the Users endpoints.

---

## 2. Game plan (Rule 2)

**Chosen approach (battle‑tested):** ASP.NET Core's canonical **permission‑based authorization** — a permission catalog in code, per‑tenant `Role`/`Permission`/`RolePermission`/`UserRole` tables, a custom `IAuthorizationPolicyProvider` that materializes a policy per permission on demand, an `AuthorizationHandler<PermissionRequirement>`, and a `[HasPermission("code")]` attribute. Permissions travel in the JWT as claims (stateless, scalable). This is the widely‑used pattern (Microsoft docs / Andrew Lock) and layers cleanly onto the existing Clean Architecture + JWT stack without adopting heavyweight ASP.NET Identity.

**Fallback / Plan B:** if permission sets ever grow large enough to bloat tokens, switch the handler to resolve permissions from a per‑user cache (keyed by a security stamp) instead of claims — same attributes/handlers, no controller changes.

---

## 3. New backend files

| File | Purpose |
|------|---------|
| [Permissions.cs](server/InventoryManagement.Domain/Authorization/Permissions.cs) | Canonical permission catalog (`resource.action` codes) + seed definitions. |
| [SystemRoles.cs](server/InventoryManagement.Domain/Authorization/SystemRoles.cs) | System role names (Administrator/Provider/Staff) + default grants. |
| [Role.cs](server/InventoryManagement.Domain/Entities/Role.cs), [Permission.cs](server/InventoryManagement.Domain/Entities/Permission.cs), [RolePermission.cs](server/InventoryManagement.Domain/Entities/RolePermission.cs), [UserRole.cs](server/InventoryManagement.Domain/Entities/UserRole.cs) | RBAC entities (tenant‑scoped via `BaseEntity`). |
| [RoleDto.cs](server/InventoryManagement.Domain/DTOs/RoleDto.cs) | `RoleDto`/`PermissionDto`/`CreateRoleDto`/`UpdateRoleDto`. |
| [IRoleRepository.cs](server/InventoryManagement.Domain/Interfaces/IRoleRepository.cs), [IPermissionRepository.cs](server/InventoryManagement.Domain/Interfaces/IPermissionRepository.cs), [IUserRoleRepository.cs](server/InventoryManagement.Domain/Interfaces/IUserRoleRepository.cs), [IRoleService.cs](server/InventoryManagement.Domain/Interfaces/IRoleService.cs) | RBAC contracts. |
| [RoleRepository.cs](server/InventoryManagement.Infrastructure/Repositories/RoleRepository.cs), [PermissionRepository.cs](server/InventoryManagement.Infrastructure/Repositories/PermissionRepository.cs), [UserRoleRepository.cs](server/InventoryManagement.Infrastructure/Repositories/UserRoleRepository.cs) | EF repositories. |
| [RoleConfiguration.cs](server/InventoryManagement.Infrastructure/Data/Configurations/RoleConfiguration.cs) + Permission/RolePermission/UserRole configs | EF mappings (per‑tenant unique indexes, provider‑safe cascade paths). |
| [RoleService.cs](server/InventoryManagement.Application/Services/RoleService.cs) | Role CRUD + permission‑grant reconciliation (system roles protected). |
| [PermissionRequirement.cs](server/InventoryManagement.API/Infrastructure/Authorization/PermissionRequirement.cs), [PermissionAuthorizationHandler.cs](server/InventoryManagement.API/Infrastructure/Authorization/PermissionAuthorizationHandler.cs), [PermissionPolicyProvider.cs](server/InventoryManagement.API/Infrastructure/Authorization/PermissionPolicyProvider.cs), [HasPermissionAttribute.cs](server/InventoryManagement.API/Infrastructure/Authorization/HasPermissionAttribute.cs) | Permission‑based authorization primitives. |
| [RolesController.cs](server/InventoryManagement.API/Controllers/RolesController.cs) | `/api/roles` — dynamic role/permission administration (`roles.manage`). |

## 4. Modified backend files (highlights)

- **Removed** `Enums/UserRole.cs`; **removed** `User.IsAdmin/IsProvider/Role`, added `User.UserRoles`.
- [AuthConstants.cs](server/InventoryManagement.Domain/Constants/AuthConstants.cs) — dropped role/flag claims + static policies; added `permission` claim.
- [TokenService.cs](server/InventoryManagement.Infrastructure/Security/TokenService.cs) — emits `ClaimTypes.Role` per role + a `permission` claim per code.
- [AuthService.cs](server/InventoryManagement.Application/Services/AuthService.cs) — pins tenant from the authenticated user, resolves roles+permissions into the token/DTO.
- [UserService.cs](server/InventoryManagement.Application/Services/UserService.cs) / [UsersController.cs](server/InventoryManagement.API/Controllers/UsersController.cs) — create/update assign roles (`RoleIds`); self‑service edits leave roles/status untouched (`RoleIds = null`); nurse‑practitioner endpoint removed.
- **All controllers** — `[Authorize(Policy = …AdminOnly/AdminOrProvider/AllRoles)]` → `[HasPermission(Permissions.X)]`; inline `IsAdmin()/IsAdminOrProvider()` → permission checks (`ApiControllerBase.HasPermission`).
- [ApiServiceCollectionExtensions.cs](server/InventoryManagement.API/Infrastructure/ApiServiceCollectionExtensions.cs) — registered `PermissionPolicyProvider` + handler.
- Seeding ([ServiceCollectionExtensions.cs](server/InventoryManagement.Infrastructure/Extensions/ServiceCollectionExtensions.cs)) — seeds catalog + system roles + grants + admin role.
- **Neutral vocabulary:** `Inventory.EquipmentName` → `Name`; DTO `equipmentName` → `name` (item) / `itemName` (denormalized child DTOs); exception messages neutralized.
- **Migrations:** all deleted; single fresh [InitialCreate](server/InventoryManagement.Infrastructure/Migrations).

## 5. Role → permission grants (seeded defaults)

| Role | Permissions |
|------|-------------|
| **Administrator** | all 16 |
| **Provider** | inventory.read/manage, stock.manage, assignments.read/manage, suppliers.read/manage, locations.read/manage, maintenance.read/manage, dashboard.read, users.read |
| **Staff** | inventory.read, suppliers.read, locations.read, maintenance.read, dashboard.read (+ own assignments via ownership) |

> Resource deletes for suppliers/locations/maintenance now require `<resource>.manage` (was Admin‑only); inventory delete keeps a dedicated `inventory.delete`. Documented behavior change; Administrator retains everything.

## 6. Frontend (updated in lockstep)

- [types.ts](client/src/lib/types.ts) — removed `UserRole`; `UserDto` → `roles`/`permissions`; create/update → `roleIds` (`null` = unchanged); `equipmentName` → `name`/`itemName`; added `RoleDto`/`PermissionDto`/`CreateRoleDto`/`UpdateRoleDto`.
- [permissions.ts](client/src/lib/permissions.ts) — permission code constants mirroring the API.
- [auth-context.tsx](client/src/lib/auth-context.tsx) — exposes `permissions` + `hasPermission(code)`; convenience flags (`isAdmin`/`isProvider`/`canManage`/`canManageUsers`) derived from permissions (so existing pages keep working).
- [api.ts](client/src/lib/api.ts) — added `api.roles.*`; removed nurse‑practitioners.
- [utils.ts](client/src/lib/utils.ts) — `roleLabels`/`roleTones` → `roleTone(name)` helper.
- [user-form-modal.tsx](client/src/components/users/user-form-modal.tsx) — dynamic role checkboxes (from `/api/roles`) instead of admin/provider switches + role dropdown.
- [status-badges.tsx](client/src/components/domain/status-badges.tsx), [users/page.tsx](client/src/app/(app)/users/page.tsx), [profile/page.tsx](client/src/app/(app)/profile/page.tsx), [sidebar.tsx](client/src/components/layout/sidebar.tsx), [user-menu.tsx](client/src/components/layout/user-menu.tsx) — render role **names**; role‑name filter.
- ~14 pages/components — `equipmentName` → `name`/`itemName`.

## 7. Verification (Rule 3)

- `dotnet build` (solution): **succeeded**; `dotnet test`: **53/53 passed**.
- Frontend `tsc --noEmit`: **clean** (0 errors). (Pre‑existing strict `react-hooks` lint warnings in untouched files are out of scope.)
- **Runtime** (fresh DB): `InitialCreate` applied; catalog (16 permissions) + 3 system roles seeded; admin → Administrator. `POST /api/auth/login` → `user.roles=[Administrator]`, `user.permissions` (16), JWT permission claims (16). `POST /api/inventory {"name":…}` → 201. `GET /api/roles` → `Administrator,Provider,Staff`; `GET /api/roles/permissions` → 16; `GET /api/auth/me` → 16 permissions.

## 8. Note on tooling

During this change a batch text‑edit intermittently failed to persist / corrupted one working‑tree file ([maintenance/page.tsx](client/src/app/(app)/maintenance/page.tsx)); it was restored from the clean committed version via `git checkout HEAD --` and the two renames re‑applied. Final state verified by `tsc`.
