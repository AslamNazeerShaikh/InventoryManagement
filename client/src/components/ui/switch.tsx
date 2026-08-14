"use client";

import { cn } from "@/lib/utils";

export function Switch({
  checked,
  onChange,
  label,
  description,
  disabled = false,
}: {
  checked: boolean;
  onChange: (checked: boolean) => void;
  label?: string;
  description?: string;
  disabled?: boolean;
}) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      disabled={disabled}
      onClick={() => onChange(!checked)}
      className={cn(
        "flex w-full items-center justify-between gap-3 rounded-xl border border-white/[0.06] bg-white/[0.02] px-3.5 py-2.5 text-left transition hover:bg-white/[0.04] disabled:opacity-50",
      )}
    >
      <span className="min-w-0">
        {label && (
          <span className="block text-sm font-medium text-slate-200">
            {label}
          </span>
        )}
        {description && (
          <span className="block text-xs text-slate-500">{description}</span>
        )}
      </span>
      <span
        className={cn(
          "relative h-6 w-11 shrink-0 rounded-full transition",
          checked ? "bg-brand-500" : "bg-white/10",
        )}
      >
        <span
          className={cn(
            "absolute left-0.5 top-0.5 size-5 rounded-full bg-white shadow transition",
            checked && "translate-x-5",
          )}
        />
      </span>
    </button>
  );
}
