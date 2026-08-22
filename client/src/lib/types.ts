/**
 * TypeScript contracts mirroring the InventoryManagement.API DTOs and enums.
 * Keep these in sync with `server/InventoryManagement.Domain`.
 */

/* ----------------------------- Enums ------------------------------ */
export enum InventoryStatus {
  Available = 1,
  Assigned = 2,
  Reserved = 3,
  Expired = 4,
  Damaged = 5,
  Disposed = 6,
}

export enum AssignmentStatus {
  Active = 1,
  Returned = 2,
  Expired = 3,
  Lost = 4,
  Damaged = 5,
}

export enum StockMovementType {
  Received = 1,
  Assigned = 2,
  Returned = 3,
  Adjusted = 4,
  Transferred = 5,
  Disposed = 6,
}

export enum MaintenanceType {
  Inspection = 1,
  Calibration = 2,
  Service = 3,
  Repair = 4,
  Cleaning = 5,
}

export enum MaintenanceStatus {
  Scheduled = 1,
  Due = 2,
  Overdue = 3,
  Completed = 4,
  Cancelled = 5,
}

export enum ReturnCondition {
  Good = 1,
  Damaged = 2,
  Lost = 3,
  NeedsRepair = 4,
}

/* --------------------------- Envelopes ---------------------------- */
export interface ApiResponse<T> {
  isSuccess: boolean;
  message: string;
  data: T | null;
  errors: string[];
}

export interface PagedResult<T> {
  data: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

/* ----------------------------- Users ------------------------------ */
export interface UserDto {
  id: number;
  name: string;
  email: string;
  roles: string[];
  permissions: string[];
  isActive: boolean;
  lastLoginAt: string | null;
  createdAt: string;
}

export interface CreateUserDto {
  name: string;
  email: string;
  password: string;
  roleIds: number[];
}

export interface UpdateUserDto {
  name: string;
  email: string;
  /** null leaves role membership unchanged (self-service profile edit). */
  roleIds: number[] | null;
  isActive: boolean;
}

/* ------------------------ Roles & permissions --------------------- */
export interface PermissionDto {
  code: string;
  description: string | null;
  category: string | null;
}

export interface RoleDto {
  id: number;
  name: string;
  description: string | null;
  isSystem: boolean;
  permissions: string[];
}

export interface CreateRoleDto {
  name: string;
  description?: string | null;
  permissions: string[];
}

export interface UpdateRoleDto {
  name: string;
  description?: string | null;
  permissions: string[];
}

/* -------------------------- Authentication ------------------------ */
export interface LoginDto {
  email: string;
  password: string;
}

export interface AuthResponseDto {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: UserDto;
}

export interface RefreshTokenDto {
  refreshToken: string;
}

export interface ChangePasswordDto {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

/* ---------------------------- Inventory --------------------------- */
export interface InventoryDto {
  id: number;
  name: string;
  description: string | null;
  category: string | null;
  brand: string | null;
  model: string | null;
  serialNumber: string | null;
  barcode: string | null;
  expiryDate: string | null;
  manufactureDate: string | null;
  purchasePrice: number | null;
  supplier: string | null;
  quantity: number;
  availableQuantity: number;
  status: InventoryStatus;
  location: string | null;
  notes: string | null;
  createdAt: string;
  createdByUserName: string | null;
  isExpiryAlertSent: boolean;
  reorderLevel: number | null;
  reorderQuantity: number | null;
  supplierId: number | null;
  supplierName: string | null;
  locationId: number | null;
  locationName: string | null;
  needsReorder: boolean;
}

export interface CreateInventoryDto {
  name: string;
  description?: string | null;
  category?: string | null;
  brand?: string | null;
  model?: string | null;
  serialNumber?: string | null;
  barcode?: string | null;
  expiryDate?: string | null;
  manufactureDate?: string | null;
  purchasePrice?: number | null;
  supplier?: string | null;
  quantity: number;
  location?: string | null;
  notes?: string | null;
  reorderLevel?: number | null;
  reorderQuantity?: number | null;
  supplierId?: number | null;
  locationId?: number | null;
}

export interface UpdateInventoryDto extends CreateInventoryDto {
  status: InventoryStatus;
}

export interface InventorySearchDto {
  searchTerm?: string | null;
  category?: string | null;
  status?: InventoryStatus | null;
  expiryDateFrom?: string | null;
  expiryDateTo?: string | null;
  pageNumber: number;
  pageSize: number;
}

/* --------------------------- Assignments -------------------------- */
export interface InventoryAssignmentDto {
  id: number;
  inventoryId: number;
  itemName: string;
  category: string | null;
  barcode: string | null;
  userId: number;
  userName: string;
  userEmail: string;
  assignedQuantity: number;
  returnedQuantity: number;
  outstandingQuantity: number;
  renewalCount: number;
  returnCondition: ReturnCondition | null;
  assignedDate: string;
  returnDate: string | null;
  expectedReturnDate: string | null;
  status: AssignmentStatus;
  assignmentNotes: string | null;
  returnNotes: string | null;
  assignedByUserName: string | null;
  returnedToUserName: string | null;
  createdAt: string;
}

export interface CreateInventoryAssignmentDto {
  inventoryId: number;
  userId: number;
  assignedQuantity: number;
  expectedReturnDate?: string | null;
  assignmentNotes?: string | null;
}

export interface UpdateInventoryAssignmentDto {
  assignedQuantity: number;
  expectedReturnDate?: string | null;
  status: AssignmentStatus;
  assignmentNotes?: string | null;
}

export interface ReturnInventoryAssignmentDto {
  assignmentId: number;
  returnQuantity?: number | null;
  returnCondition?: ReturnCondition | null;
  returnNotes?: string | null;
}

export interface RenewInventoryAssignmentDto {
  assignmentId: number;
  newExpectedReturnDate: string;
  notes?: string | null;
}

export interface AssignmentHistoryDto {
  inventoryId: number;
  itemName: string;
  assignments: InventoryAssignmentDto[];
}

/* ---------------------------- Dashboard --------------------------- */
export interface DashboardStatsDto {
  totalInventories: number;
  availableInventories: number;
  assignedInventories: number;
  expiringInventories: number;
  lowStockInventories: number;
  totalUsers: number;
  activeAssignments: number;
  overdueAssignments: number;
}

export interface AlertsSummaryDto {
  expiryAlerts: InventoryDto[];
  expiryCount: number;
  lowStockAlerts: InventoryDto[];
  lowStockCount: number;
  overdueAlerts: InventoryAssignmentDto[];
  overdueCount: number;
  hasPermissionForOverdue: boolean;
}

/* ------------------------- Stock movements ------------------------ */
export interface StockMovementDto {
  id: number;
  inventoryId: number;
  itemName: string;
  movementType: StockMovementType;
  quantityChange: number;
  balanceAfter: number;
  reason: string | null;
  notes: string | null;
  unitCost: number | null;
  performedByUserName: string | null;
  assignmentId: number | null;
  fromLocationName: string | null;
  toLocationName: string | null;
  supplierName: string | null;
  createdAt: string;
}

export interface ReceiveStockDto {
  quantity: number;
  unitCost?: number | null;
  supplierId?: number | null;
  reason?: string | null;
  notes?: string | null;
}

export interface AdjustStockDto {
  quantityDelta: number;
  reason: string;
  notes?: string | null;
}

export interface DisposeStockDto {
  quantity: number;
  reason: string;
  notes?: string | null;
}

export interface TransferStockDto {
  toLocationId: number;
  reason?: string | null;
  notes?: string | null;
}

/* ----------------------------- Suppliers -------------------------- */
export interface SupplierDto {
  id: number;
  name: string;
  contactName: string | null;
  email: string | null;
  phone: string | null;
  address: string | null;
  website: string | null;
  leadTimeDays: number | null;
  isActive: boolean;
  notes: string | null;
  itemCount: number;
  createdAt: string;
}

export interface CreateSupplierDto {
  name: string;
  contactName?: string | null;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
  website?: string | null;
  leadTimeDays?: number | null;
  notes?: string | null;
}

export interface UpdateSupplierDto extends CreateSupplierDto {
  isActive: boolean;
}

/* ----------------------------- Locations -------------------------- */
export interface LocationDto {
  id: number;
  name: string;
  code: string | null;
  description: string | null;
  parentLocationId: number | null;
  parentLocationName: string | null;
  isActive: boolean;
  itemCount: number;
  createdAt: string;
}

export interface CreateLocationDto {
  name: string;
  code?: string | null;
  description?: string | null;
  parentLocationId?: number | null;
}

export interface UpdateLocationDto extends CreateLocationDto {
  isActive: boolean;
}

/* --------------------------- Maintenance -------------------------- */
export interface MaintenanceScheduleDto {
  id: number;
  inventoryId: number;
  itemName: string;
  maintenanceType: MaintenanceType;
  title: string;
  description: string | null;
  intervalDays: number | null;
  lastPerformedAt: string | null;
  nextDueAt: string;
  status: MaintenanceStatus;
  performedByUserName: string | null;
  notes: string | null;
  createdAt: string;
}

export interface CreateMaintenanceScheduleDto {
  inventoryId: number;
  maintenanceType: MaintenanceType;
  title: string;
  description?: string | null;
  intervalDays?: number | null;
  nextDueAt: string;
  notes?: string | null;
}

export interface UpdateMaintenanceScheduleDto {
  maintenanceType: MaintenanceType;
  title: string;
  description?: string | null;
  intervalDays?: number | null;
  nextDueAt: string;
  status: MaintenanceStatus;
  notes?: string | null;
}

export interface CompleteMaintenanceDto {
  performedAt?: string | null;
  nextDueAt?: string | null;
  notes?: string | null;
}
