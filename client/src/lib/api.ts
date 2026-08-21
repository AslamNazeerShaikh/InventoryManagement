import { API_BASE_URL, STORAGE_KEYS } from "@/lib/config";
import type {
  AdjustStockDto,
  AlertsSummaryDto,
  ApiResponse,
  AssignmentHistoryDto,
  AuthResponseDto,
  ChangePasswordDto,
  CompleteMaintenanceDto,
  CreateInventoryAssignmentDto,
  CreateInventoryDto,
  CreateLocationDto,
  CreateMaintenanceScheduleDto,
  CreateSupplierDto,
  CreateUserDto,
  DashboardStatsDto,
  DisposeStockDto,
  InventoryAssignmentDto,
  InventoryDto,
  InventorySearchDto,
  LocationDto,
  LoginDto,
  MaintenanceScheduleDto,
  PagedResult,
  ReceiveStockDto,
  RenewInventoryAssignmentDto,
  ReturnInventoryAssignmentDto,
  StockMovementDto,
  SupplierDto,
  TransferStockDto,
  UpdateInventoryAssignmentDto,
  UpdateInventoryDto,
  UpdateLocationDto,
  UpdateMaintenanceScheduleDto,
  UpdateSupplierDto,
  UpdateUserDto,
  UserDto,
} from "@/lib/types";

/** Error thrown for any non-successful API outcome, carrying HTTP + envelope detail. */
export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly errors: string[] = [],
  ) {
    super(message);
    this.name = "ApiError";
  }
}

/* -------------------------- Token storage ------------------------- */
let accessToken: string | null = null;
let refreshToken: string | null = null;
let onUnauthorized: (() => void) | null = null;

const isBrowser = typeof window !== "undefined";

function hydrate() {
  if (!isBrowser) return;
  accessToken = localStorage.getItem(STORAGE_KEYS.accessToken);
  refreshToken = localStorage.getItem(STORAGE_KEYS.refreshToken);
}
hydrate();

export function setSession(auth: AuthResponseDto) {
  accessToken = auth.accessToken;
  refreshToken = auth.refreshToken;
  if (isBrowser) {
    localStorage.setItem(STORAGE_KEYS.accessToken, auth.accessToken);
    localStorage.setItem(STORAGE_KEYS.refreshToken, auth.refreshToken);
    localStorage.setItem(STORAGE_KEYS.expiresAt, auth.expiresAt);
    localStorage.setItem(STORAGE_KEYS.user, JSON.stringify(auth.user));
  }
}

export function clearSession() {
  accessToken = null;
  refreshToken = null;
  if (isBrowser) {
    Object.values(STORAGE_KEYS).forEach((k) => localStorage.removeItem(k));
  }
}

export function getStoredUser(): UserDto | null {
  if (!isBrowser) return null;
  const raw = localStorage.getItem(STORAGE_KEYS.user);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as UserDto;
  } catch {
    return null;
  }
}

export function hasSession(): boolean {
  return Boolean(accessToken && refreshToken);
}

export function registerUnauthorizedHandler(handler: () => void) {
  onUnauthorized = handler;
}

/* --------------------------- Core request ------------------------- */
type QueryValue = string | number | boolean | null | undefined;

interface RequestOptions {
  query?: Record<string, QueryValue>;
  body?: unknown;
  idempotencyKey?: string;
  auth?: boolean;
  _retry?: boolean;
}

function buildUrl(path: string, query?: Record<string, QueryValue>): string {
  const base = `${API_BASE_URL}${path}`;
  if (!query) return base;
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (value !== null && value !== undefined && value !== "") {
      params.append(key, String(value));
    }
  }
  const qs = params.toString();
  return qs ? `${base}?${qs}` : base;
}

/** Abort any single API request that exceeds this many milliseconds. */
const REQUEST_TIMEOUT_MS = 30_000;

let refreshPromise: Promise<boolean> | null = null;

async function tryRefresh(): Promise<boolean> {
  if (!refreshToken) return false;
  if (!refreshPromise) {
    refreshPromise = (async () => {
      try {
        const res = await fetch(buildUrl("/api/auth/refresh"), {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ refreshToken }),
        });
        const json = (await res.json()) as ApiResponse<AuthResponseDto>;
        if (res.ok && json.isSuccess && json.data) {
          setSession(json.data);
          return true;
        }
      } catch {
        /* network failure — fall through to failure */
      }
      return false;
    })().finally(() => {
      refreshPromise = null;
    });
  }
  return refreshPromise;
}

async function request<T>(
  method: string,
  path: string,
  options: RequestOptions = {},
): Promise<T> {
  const { query, body, idempotencyKey, auth = true, _retry = false } = options;
  const headers: Record<string, string> = { Accept: "application/json" };
  if (body !== undefined) headers["Content-Type"] = "application/json";
  if (auth && accessToken) headers.Authorization = `Bearer ${accessToken}`;
  if (idempotencyKey) headers["Idempotency-Key"] = idempotencyKey;

  let res: Response;
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);
  try {
    res = await fetch(buildUrl(path, query), {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
      signal: controller.signal,
      // Never let the browser cache authenticated API responses.
      cache: "no-store",
    });
  } catch (e) {
    const timedOut = e instanceof DOMException && e.name === "AbortError";
    throw new ApiError(
      timedOut
        ? "The request timed out. Please try again."
        : "Cannot reach the API. Is the InventoryManagement.API server running?",
      0,
    );
  } finally {
    clearTimeout(timeoutId);
  }

  if (res.status === 401 && auth && !_retry && refreshToken) {
    const refreshed = await tryRefresh();
    if (refreshed) {
      return request<T>(method, path, { ...options, _retry: true });
    }
    clearSession();
    onUnauthorized?.();
    throw new ApiError("Your session has expired. Please sign in again.", 401);
  }

  let json: ApiResponse<T> | null = null;
  const text = await res.text();
  if (text) {
    try {
      json = JSON.parse(text) as ApiResponse<T>;
    } catch {
      json = null;
    }
  }

  if (!res.ok || (json && !json.isSuccess)) {
    const message =
      json?.message ||
      (res.status === 429
        ? "Too many attempts. Please wait a moment and try again."
        : `Request failed (${res.status})`);
    throw new ApiError(message, res.status, json?.errors ?? []);
  }

  return (json?.data as T) ?? (undefined as T);
}

const get = <T>(path: string, query?: Record<string, QueryValue>, auth = true) =>
  request<T>("GET", path, { query, auth });
const post = <T>(
  path: string,
  body?: unknown,
  opts: Omit<RequestOptions, "body"> = {},
) => request<T>("POST", path, { ...opts, body });
const put = <T>(path: string, body?: unknown, opts: RequestOptions = {}) =>
  request<T>("PUT", path, { ...opts, body });
const del = <T>(path: string, opts: RequestOptions = {}) =>
  request<T>("DELETE", path, opts);

/* ---------------------------- Endpoints --------------------------- */
export const api = {
  auth: {
    login: (dto: LoginDto) =>
      post<AuthResponseDto>("/api/auth/login", dto, { auth: false }),
    logout: () => post<boolean>("/api/auth/logout"),
    changePassword: (dto: ChangePasswordDto) =>
      post<boolean>("/api/auth/change-password", dto),
    me: () =>
      get<Record<string, string | null>>("/api/auth/me"),
  },

  users: {
    list: () => get<UserDto[]>("/api/users"),
    paged: (pageNumber: number, pageSize: number) =>
      get<PagedResult<UserDto>>("/api/users/paged", { pageNumber, pageSize }),
    byId: (id: number) => get<UserDto>(`/api/users/${id}`),
    byEmail: (email: string) =>
      get<UserDto>(`/api/users/by-email/${encodeURIComponent(email)}`),
    create: (dto: CreateUserDto) => post<UserDto>("/api/users", dto),
    update: (id: number, dto: UpdateUserDto) =>
      put<UserDto>(`/api/users/${id}`, dto),
    remove: (id: number) => del<boolean>(`/api/users/${id}`),
    nursePractitioners: () => get<UserDto[]>("/api/users/nurse-practitioners"),
    active: () => get<UserDto[]>("/api/users/active"),
  },

  inventory: {
    list: () => get<InventoryDto[]>("/api/inventory"),
    paged: (pageNumber: number, pageSize: number) =>
      get<PagedResult<InventoryDto>>("/api/inventory/paged", {
        pageNumber,
        pageSize,
      }),
    byId: (id: number) => get<InventoryDto>(`/api/inventory/${id}`),
    byBarcode: (barcode: string) =>
      get<InventoryDto>(`/api/inventory/barcode/${encodeURIComponent(barcode)}`),
    search: (dto: InventorySearchDto) =>
      post<InventoryDto[]>("/api/inventory/search", dto),
    available: () => get<InventoryDto[]>("/api/inventory/available"),
    expiring: (monthsBefore = 3) =>
      get<InventoryDto[]>("/api/inventory/expiring", { monthsBefore }),
    lowStock: (threshold = 5) =>
      get<InventoryDto[]>("/api/inventory/low-stock", { threshold }),
    byCategory: (category: string) =>
      get<InventoryDto[]>(`/api/inventory/category/${encodeURIComponent(category)}`),
    create: (dto: CreateInventoryDto, idempotencyKey?: string) =>
      post<InventoryDto>("/api/inventory", dto, { idempotencyKey }),
    update: (id: number, dto: UpdateInventoryDto, idempotencyKey?: string) =>
      put<InventoryDto>(`/api/inventory/${id}`, dto, { idempotencyKey }),
    remove: (id: number) => del<boolean>(`/api/inventory/${id}`),
    reorder: () => get<InventoryDto[]>("/api/inventory/reorder"),
    movements: (id: number) =>
      get<StockMovementDto[]>(`/api/inventory/${id}/movements`),
    recentMovements: (pageNumber: number, pageSize: number) =>
      get<PagedResult<StockMovementDto>>("/api/inventory/movements/recent", {
        pageNumber,
        pageSize,
      }),
    receive: (id: number, dto: ReceiveStockDto, idempotencyKey?: string) =>
      post<InventoryDto>(`/api/inventory/${id}/receive`, dto, { idempotencyKey }),
    adjust: (id: number, dto: AdjustStockDto, idempotencyKey?: string) =>
      post<InventoryDto>(`/api/inventory/${id}/adjust`, dto, { idempotencyKey }),
    dispose: (id: number, dto: DisposeStockDto, idempotencyKey?: string) =>
      post<InventoryDto>(`/api/inventory/${id}/dispose`, dto, { idempotencyKey }),
    transfer: (id: number, dto: TransferStockDto, idempotencyKey?: string) =>
      post<InventoryDto>(`/api/inventory/${id}/transfer`, dto, { idempotencyKey }),
  },

  assignments: {
    list: () => get<InventoryAssignmentDto[]>("/api/inventoryassignments"),
    paged: (pageNumber: number, pageSize: number) =>
      get<PagedResult<InventoryAssignmentDto>>("/api/inventoryassignments/paged", {
        pageNumber,
        pageSize,
      }),
    byId: (id: number) =>
      get<InventoryAssignmentDto>(`/api/inventoryassignments/${id}`),
    byUser: (userId: number) =>
      get<InventoryAssignmentDto[]>(`/api/inventoryassignments/user/${userId}`),
    mine: () =>
      get<InventoryAssignmentDto[]>("/api/inventoryassignments/my-assignments"),
    active: () =>
      get<InventoryAssignmentDto[]>("/api/inventoryassignments/active"),
    activeByUser: (userId: number) =>
      get<InventoryAssignmentDto[]>(
        `/api/inventoryassignments/active/user/${userId}`,
      ),
    overdue: () =>
      get<InventoryAssignmentDto[]>("/api/inventoryassignments/overdue"),
    history: (inventoryId: number) =>
      get<AssignmentHistoryDto>(
        `/api/inventoryassignments/history/inventory/${inventoryId}`,
      ),
    create: (dto: CreateInventoryAssignmentDto, idempotencyKey?: string) =>
      post<InventoryAssignmentDto>("/api/inventoryassignments", dto, {
        idempotencyKey,
      }),
    update: (id: number, dto: UpdateInventoryAssignmentDto) =>
      put<InventoryAssignmentDto>(`/api/inventoryassignments/${id}`, dto),
    return: (dto: ReturnInventoryAssignmentDto, idempotencyKey?: string) =>
      post<boolean>("/api/inventoryassignments/return", dto, { idempotencyKey }),
    renew: (dto: RenewInventoryAssignmentDto) =>
      post<InventoryAssignmentDto>("/api/inventoryassignments/renew", dto),
    dueSoon: (daysAhead = 7) =>
      get<InventoryAssignmentDto[]>("/api/inventoryassignments/due-soon", {
        daysAhead,
      }),
  },

  suppliers: {
    list: (activeOnly = false) =>
      get<SupplierDto[]>("/api/suppliers", { activeOnly }),
    paged: (pageNumber: number, pageSize: number) =>
      get<PagedResult<SupplierDto>>("/api/suppliers/paged", {
        pageNumber,
        pageSize,
      }),
    byId: (id: number) => get<SupplierDto>(`/api/suppliers/${id}`),
    create: (dto: CreateSupplierDto) => post<SupplierDto>("/api/suppliers", dto),
    update: (id: number, dto: UpdateSupplierDto) =>
      put<SupplierDto>(`/api/suppliers/${id}`, dto),
    remove: (id: number) => del<boolean>(`/api/suppliers/${id}`),
  },

  locations: {
    list: (activeOnly = false) =>
      get<LocationDto[]>("/api/locations", { activeOnly }),
    paged: (pageNumber: number, pageSize: number) =>
      get<PagedResult<LocationDto>>("/api/locations/paged", {
        pageNumber,
        pageSize,
      }),
    byId: (id: number) => get<LocationDto>(`/api/locations/${id}`),
    create: (dto: CreateLocationDto) => post<LocationDto>("/api/locations", dto),
    update: (id: number, dto: UpdateLocationDto) =>
      put<LocationDto>(`/api/locations/${id}`, dto),
    remove: (id: number) => del<boolean>(`/api/locations/${id}`),
  },

  maintenance: {
    paged: (pageNumber: number, pageSize: number) =>
      get<PagedResult<MaintenanceScheduleDto>>("/api/maintenance/paged", {
        pageNumber,
        pageSize,
      }),
    due: (daysAhead = 30) =>
      get<MaintenanceScheduleDto[]>("/api/maintenance/due", { daysAhead }),
    byInventory: (inventoryId: number) =>
      get<MaintenanceScheduleDto[]>(`/api/maintenance/inventory/${inventoryId}`),
    byId: (id: number) => get<MaintenanceScheduleDto>(`/api/maintenance/${id}`),
    create: (dto: CreateMaintenanceScheduleDto) =>
      post<MaintenanceScheduleDto>("/api/maintenance", dto),
    update: (id: number, dto: UpdateMaintenanceScheduleDto) =>
      put<MaintenanceScheduleDto>(`/api/maintenance/${id}`, dto),
    complete: (id: number, dto: CompleteMaintenanceDto) =>
      post<MaintenanceScheduleDto>(`/api/maintenance/${id}/complete`, dto),
    remove: (id: number) => del<boolean>(`/api/maintenance/${id}`),
  },

  dashboard: {
    stats: () => get<DashboardStatsDto>("/api/dashboard/stats"),
    recentInventories: (count = 5) =>
      get<InventoryDto[]>("/api/dashboard/recent-inventories", { count }),
    recentAssignments: (count = 5) =>
      get<InventoryAssignmentDto[]>("/api/dashboard/recent-assignments", {
        count,
      }),
    expiryAlerts: () =>
      get<InventoryDto[]>("/api/dashboard/alerts/expiry"),
    lowStockAlerts: () =>
      get<InventoryDto[]>("/api/dashboard/alerts/low-stock"),
    overdueAlerts: () =>
      get<InventoryAssignmentDto[]>("/api/dashboard/alerts/overdue"),
    alertsSummary: () => get<AlertsSummaryDto>("/api/dashboard/alerts/summary"),
  },

  health: async (): Promise<boolean> => {
    try {
      const res = await fetch(buildUrl("/api/health"), { method: "GET" });
      return res.ok;
    } catch {
      return false;
    }
  },
};
