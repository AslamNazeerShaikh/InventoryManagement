import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";
import {
  AssignmentStatus,
  InventoryStatus,
  MaintenanceStatus,
  MaintenanceType,
  ReturnCondition,
  StockMovementType,
  UserRole,
} from "@/lib/types";

/** Merge Tailwind classes with conflict resolution. */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

/** Semantic colour tones used across badges, chips and accents. */
export type Tone =
  | "brand"
  | "success"
  | "warning"
  | "danger"
  | "info"
  | "violet"
  | "neutral";

/* ------------------------------ Dates ----------------------------- */
function toDate(value: string | Date | null | undefined): Date | null {
  if (!value) return null;
  const d = value instanceof Date ? value : new Date(value);
  return Number.isNaN(d.getTime()) ? null : d;
}

export function formatDate(value: string | Date | null | undefined): string {
  const d = toDate(value);
  if (!d) return "—";
  return d.toLocaleDateString("en-US", {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

export function formatDateTime(value: string | Date | null | undefined): string {
  const d = toDate(value);
  if (!d) return "—";
  return d.toLocaleString("en-US", {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  });
}

export function formatRelativeTime(
  value: string | Date | null | undefined,
): string {
  const d = toDate(value);
  if (!d) return "—";
  const diffMs = d.getTime() - Date.now();
  const abs = Math.abs(diffMs);
  const units: [Intl.RelativeTimeFormatUnit, number][] = [
    ["year", 1000 * 60 * 60 * 24 * 365],
    ["month", 1000 * 60 * 60 * 24 * 30],
    ["day", 1000 * 60 * 60 * 24],
    ["hour", 1000 * 60 * 60],
    ["minute", 1000 * 60],
  ];
  const rtf = new Intl.RelativeTimeFormat("en", { numeric: "auto" });
  for (const [unit, ms] of units) {
    if (abs >= ms || unit === "minute") {
      return rtf.format(Math.round(diffMs / ms), unit);
    }
  }
  return "just now";
}

/** Whole days until `value` (negative when in the past). */
export function daysUntil(value: string | Date | null | undefined): number | null {
  const d = toDate(value);
  if (!d) return null;
  const start = new Date();
  start.setHours(0, 0, 0, 0);
  const target = new Date(d);
  target.setHours(0, 0, 0, 0);
  return Math.round((target.getTime() - start.getTime()) / (1000 * 60 * 60 * 24));
}

/** For <input type="date"> — yyyy-MM-dd in local time. */
export function toDateInputValue(
  value: string | Date | null | undefined,
): string {
  const d = toDate(value);
  if (!d) return "";
  const off = d.getTimezoneOffset();
  return new Date(d.getTime() - off * 60000).toISOString().slice(0, 10);
}

/** Turn a date-only input into an ISO UTC string for the API. */
export function dateInputToIso(value: string): string | null {
  if (!value) return null;
  const d = new Date(`${value}T00:00:00Z`);
  return Number.isNaN(d.getTime()) ? null : d.toISOString();
}

/* ---------------------------- Numbers ----------------------------- */
export function formatCurrency(value: number | null | undefined): string {
  if (value == null) return "—";
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    maximumFractionDigits: 2,
  }).format(value);
}

export function formatNumber(value: number | null | undefined): string {
  if (value == null) return "0";
  return new Intl.NumberFormat("en-US").format(value);
}

/* ----------------------------- Strings ---------------------------- */
export function getInitials(name: string | null | undefined): string {
  if (!name) return "?";
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "?";
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

/* -------------------------- Enum display -------------------------- */
export const roleLabels: Record<UserRole, string> = {
  [UserRole.Admin]: "Administrator",
  [UserRole.NursePractitioner]: "Nurse Practitioner",
  [UserRole.Staff]: "Staff",
};

export const roleTones: Record<UserRole, Tone> = {
  [UserRole.Admin]: "violet",
  [UserRole.NursePractitioner]: "brand",
  [UserRole.Staff]: "neutral",
};

export const inventoryStatusLabels: Record<InventoryStatus, string> = {
  [InventoryStatus.Available]: "Available",
  [InventoryStatus.Assigned]: "Assigned",
  [InventoryStatus.Reserved]: "Reserved",
  [InventoryStatus.Expired]: "Expired",
  [InventoryStatus.Damaged]: "Damaged",
  [InventoryStatus.Disposed]: "Disposed",
};

export const inventoryStatusTones: Record<InventoryStatus, Tone> = {
  [InventoryStatus.Available]: "success",
  [InventoryStatus.Assigned]: "info",
  [InventoryStatus.Reserved]: "warning",
  [InventoryStatus.Expired]: "danger",
  [InventoryStatus.Damaged]: "danger",
  [InventoryStatus.Disposed]: "neutral",
};

export const assignmentStatusLabels: Record<AssignmentStatus, string> = {
  [AssignmentStatus.Active]: "Active",
  [AssignmentStatus.Returned]: "Returned",
  [AssignmentStatus.Expired]: "Expired",
  [AssignmentStatus.Lost]: "Lost",
  [AssignmentStatus.Damaged]: "Damaged",
};

export const assignmentStatusTones: Record<AssignmentStatus, Tone> = {
  [AssignmentStatus.Active]: "info",
  [AssignmentStatus.Returned]: "success",
  [AssignmentStatus.Expired]: "warning",
  [AssignmentStatus.Lost]: "danger",
  [AssignmentStatus.Damaged]: "danger",
};

export const stockMovementLabels: Record<StockMovementType, string> = {
  [StockMovementType.Received]: "Received",
  [StockMovementType.Assigned]: "Assigned",
  [StockMovementType.Returned]: "Returned",
  [StockMovementType.Adjusted]: "Adjusted",
  [StockMovementType.Transferred]: "Transferred",
  [StockMovementType.Disposed]: "Disposed",
};

export const stockMovementTones: Record<StockMovementType, Tone> = {
  [StockMovementType.Received]: "success",
  [StockMovementType.Assigned]: "info",
  [StockMovementType.Returned]: "brand",
  [StockMovementType.Adjusted]: "warning",
  [StockMovementType.Transferred]: "violet",
  [StockMovementType.Disposed]: "danger",
};

export const maintenanceTypeLabels: Record<MaintenanceType, string> = {
  [MaintenanceType.Inspection]: "Inspection",
  [MaintenanceType.Calibration]: "Calibration",
  [MaintenanceType.Service]: "Service",
  [MaintenanceType.Repair]: "Repair",
  [MaintenanceType.Cleaning]: "Cleaning",
};

export const maintenanceStatusLabels: Record<MaintenanceStatus, string> = {
  [MaintenanceStatus.Scheduled]: "Scheduled",
  [MaintenanceStatus.Due]: "Due",
  [MaintenanceStatus.Overdue]: "Overdue",
  [MaintenanceStatus.Completed]: "Completed",
  [MaintenanceStatus.Cancelled]: "Cancelled",
};

export const maintenanceStatusTones: Record<MaintenanceStatus, Tone> = {
  [MaintenanceStatus.Scheduled]: "info",
  [MaintenanceStatus.Due]: "warning",
  [MaintenanceStatus.Overdue]: "danger",
  [MaintenanceStatus.Completed]: "success",
  [MaintenanceStatus.Cancelled]: "neutral",
};

export const returnConditionLabels: Record<ReturnCondition, string> = {
  [ReturnCondition.Good]: "Good",
  [ReturnCondition.Damaged]: "Damaged",
  [ReturnCondition.Lost]: "Lost",
  [ReturnCondition.NeedsRepair]: "Needs repair",
};

export const returnConditionTones: Record<ReturnCondition, Tone> = {
  [ReturnCondition.Good]: "success",
  [ReturnCondition.Damaged]: "danger",
  [ReturnCondition.Lost]: "danger",
  [ReturnCondition.NeedsRepair]: "warning",
};

export function enumOptions<T extends Record<string, string | number>>(
  e: T,
  labels: Record<number, string>,
): { value: number; label: string }[] {
  return Object.values(e)
    .filter((v): v is number => typeof v === "number")
    .map((value) => ({ value, label: labels[value] ?? String(value) }));
}
