/**
 * TypeScript contracts mirroring the InventoryManagement.API DTOs and enums.
 * Keep these in sync with `server/InventoryManagement.Domain`.
 */

/* ----------------------------- Enums ------------------------------ */
export enum UserRole {
  Admin = 1,
  NursePractitioner = 2,
  Staff = 3,
}

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
  isAdmin: boolean;
  isProvider: boolean;
  role: UserRole;
  isActive: boolean;
  lastLoginAt: string | null;
  createdAt: string;
}

export interface CreateUserDto {
  name: string;
  email: string;
  password: string;
  isAdmin: boolean;
  isProvider: boolean;
  role: UserRole;
}

export interface UpdateUserDto {
  name: string;
  email: string;
  isAdmin: boolean;
  isProvider: boolean;
  role: UserRole;
  isActive: boolean;
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
  equipmentName: string;
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
}

export interface CreateInventoryDto {
  equipmentName: string;
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
  equipmentName: string;
  category: string | null;
  barcode: string | null;
  userId: number;
  userName: string;
  userEmail: string;
  assignedQuantity: number;
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
  returnNotes?: string | null;
}

export interface AssignmentHistoryDto {
  inventoryId: number;
  equipmentName: string;
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
