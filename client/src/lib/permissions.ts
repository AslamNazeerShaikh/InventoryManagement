/**
 * Permission codes mirroring the API's authorization catalog (server `Permissions`). Used with
 * `useAuth().hasPermission(...)` to gate UI. Tenant-defined custom permissions can also be checked
 * by their raw string code.
 */
export const PERMISSIONS = {
  inventory: {
    read: "inventory.read",
    manage: "inventory.manage",
    delete: "inventory.delete",
  },
  stock: { manage: "stock.manage" },
  assignments: { read: "assignments.read", manage: "assignments.manage" },
  suppliers: { read: "suppliers.read", manage: "suppliers.manage" },
  locations: { read: "locations.read", manage: "locations.manage" },
  maintenance: { read: "maintenance.read", manage: "maintenance.manage" },
  dashboard: { read: "dashboard.read" },
  users: { read: "users.read", manage: "users.manage" },
  roles: { manage: "roles.manage" },
} as const;
