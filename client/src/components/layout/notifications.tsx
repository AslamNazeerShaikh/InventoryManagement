"use client";

import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import { AnimatePresence, motion } from "motion/react";
import {
  Bell,
  CalendarClock,
  ShieldAlert,
  TrendingDown,
  type LucideIcon,
} from "lucide-react";
import { api } from "@/lib/api";
import type { AlertsSummaryDto } from "@/lib/types";
import { cn } from "@/lib/utils";

/**
 * Topbar alerts bell. Polls GET /api/dashboard/alerts/summary (one call for
 * expiry + low-stock + overdue counts; overdue only for privileged callers)
 * and links each alert group to the relevant screen.
 */
export function Notifications() {
  const [open, setOpen] = useState(false);
  const [summary, setSummary] = useState<AlertsSummaryDto | null>(null);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    let active = true;
    const load = async () => {
      try {
        const s = await api.dashboard.alertsSummary();
        if (active) setSummary(s);
      } catch {
        /* non-fatal: the bell simply shows no alerts */
      }
    };
    load();
    const id = setInterval(load, 60000);
    return () => {
      active = false;
      clearInterval(id);
    };
  }, []);

  useEffect(() => {
    function onClick(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    }
    document.addEventListener("mousedown", onClick);
    return () => document.removeEventListener("mousedown", onClick);
  }, []);

  const total = summary
    ? summary.expiryCount + summary.lowStockCount + summary.overdueCount
    : 0;

  return (
    <div ref={ref} className="relative">
      <button
        onClick={() => setOpen((o) => !o)}
        title="Alerts"
        aria-label="Alerts"
        className="relative flex size-9 items-center justify-center rounded-xl text-slate-300 transition hover:bg-white/[0.06] hover:text-white"
      >
        <Bell className="size-5" />
        {total > 0 && (
          <span className="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-rose-500 px-1 text-[10px] font-semibold text-white">
            {total > 99 ? "99+" : total}
          </span>
        )}
      </button>

      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, y: 8, scale: 0.97 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: 6, scale: 0.98 }}
            transition={{ duration: 0.15 }}
            className="panel absolute right-0 z-50 mt-2 w-80 rounded-2xl p-2"
          >
            <div className="px-3 py-2">
              <p className="text-sm font-semibold text-white">Alerts</p>
              <p className="text-xs text-slate-500">
                {total === 0
                  ? "You're all caught up."
                  : `${total} item${total === 1 ? "" : "s"} need attention.`}
              </p>
            </div>
            <div className="my-1 h-px bg-white/[0.06]" />
            <AlertRow
              href="/inventory"
              icon={CalendarClock}
              tone="warning"
              label="Expiring soon"
              count={summary?.expiryCount ?? 0}
              onNavigate={() => setOpen(false)}
            />
            <AlertRow
              href="/inventory"
              icon={TrendingDown}
              tone="danger"
              label="Low stock"
              count={summary?.lowStockCount ?? 0}
              onNavigate={() => setOpen(false)}
            />
            {summary?.hasPermissionForOverdue && (
              <AlertRow
                href="/assignments"
                icon={ShieldAlert}
                tone="danger"
                label="Overdue returns"
                count={summary?.overdueCount ?? 0}
                onNavigate={() => setOpen(false)}
              />
            )}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}

function AlertRow({
  href,
  icon: Icon,
  tone,
  label,
  count,
  onNavigate,
}: {
  href: string;
  icon: LucideIcon;
  tone: "warning" | "danger";
  label: string;
  count: number;
  onNavigate: () => void;
}) {
  const toneText = tone === "warning" ? "text-amber-300" : "text-rose-300";
  const toneBg = tone === "warning" ? "bg-amber-500/10" : "bg-rose-500/10";
  return (
    <Link
      href={href}
      onClick={onNavigate}
      className="flex items-center gap-3 rounded-xl px-3 py-2 transition hover:bg-white/[0.06]"
    >
      <span
        className={cn("flex size-8 items-center justify-center rounded-lg", toneBg)}
      >
        <Icon className={cn("size-4", toneText)} />
      </span>
      <span className="flex-1 text-sm text-slate-200">{label}</span>
      <span
        className={cn(
          "text-sm font-semibold",
          count > 0 ? toneText : "text-slate-500",
        )}
      >
        {count}
      </span>
    </Link>
  );
}
