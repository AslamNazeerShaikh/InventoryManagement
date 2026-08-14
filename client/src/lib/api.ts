import { API_BASE_URL, STORAGE_KEYS } from "@/lib/config";
import type {
  AlertsSummaryDto,
  ApiResponse,
  AssignmentHistoryDto,
  AuthResponseDto,
  ChangePasswordDto,
  CreateInventoryAssignmentDto,
  CreateInventoryDto,
  CreateUserDto,
  DashboardStatsDto,
  InventoryAssignmentDto,
  InventoryDto,
  InventorySearchDto,
  LoginDto,
  PagedResult,
  ReturnInventoryAssignmentDto,
  UpdateInventoryAssignmentDto,
  UpdateInventoryDto,
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
  try {
    res = await fetch(buildUrl(path, query), {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  } catch {
    throw new ApiError(
      "Cannot reach the API. Is the InventoryManagement.API server running?",
      0,
    );
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
