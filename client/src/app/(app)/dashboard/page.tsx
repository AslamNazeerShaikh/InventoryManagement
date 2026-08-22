"use client";

import { useState } from "react";
import Link from "next/link";
import {
  AlertTriangle,
  ArrowUpRight,
  Boxes,
  CalendarClock,
  ClipboardList,
  Clock,
  PackageCheck,
  ShieldAlert,
  TrendingDown,
  Users,
} from "lucide-react";
import { api } from "@/lib/api";
import { useAsync } from "@/lib/use-async";
import { useAuth } from "@/lib/auth-context";
import type { InventoryAssignmentDto, InventoryDto } from "@/lib/types";
import {
  cn,
  daysUntil,
  formatDate,
  formatRelativeTime,
} from "@/lib/utils";
import { PageHeader } from "@/components/layout/page-header";
import { StatCard } from "@/components/dashboard/stat-card";
import { Donut } from "@/components/dashboard/donut";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { ErrorState } from "@/components/ui/error-state";
import { Avatar } from "@/components/ui/avatar";
import { FadeIn, Stagger, StaggerItem } from "@/components/ui/reveal";

export default function DashboardPage() {
  const { user, canManage } = useAuth();

  // Tunable alert parameters (backend clamps the expiry window to 3–6 months).
  const [expiryMonths, setExpiryMonths] = useState(3);
  const [lowStockThreshold, setLowStockThreshold] = useState(5);

  const { data, loading, error, reload } = useAsync(async () => {
    const [stats, recentInventories] = await Promise.all([
      api.dashboard.stats(),
      api.dashboard.recentInventories(6),
    ]);

    let recentAssignments: InventoryAssignmentDto[] = [];
    let overdue: InventoryAssignmentDto[] = [];
    if (canManage) {
      [recentAssignments, overdue] = await Promise.all([
        api.dashboard.recentAssignments(6),
        api.dashboard.overdueAlerts(),
      ]);
    }
    return { stats, recentInventories, recentAssignments, overdue };
  }, [canManage]);

  // Expiry window and low-stock threshold are queried independently so tuning a
  // control re-hits the server for just that list, not the whole dashboard.
  const expiring = useAsync(
    () => api.inventory.expiring(expiryMonths),
    [expiryMonths],
  );
  const lowStock = useAsync(
    () => api.inventory.lowStock(lowStockThreshold),
    [lowStockThreshold],
  );

  const firstName = user?.name?.split(" ")[0] ?? "there";

  return (
    <div className="space-y-8">
      <PageHeader
        title={`Welcome back, ${firstName}`}
        description={new Date().toLocaleDateString("en-US", {
          weekday: "long",
          month: "long",
          day: "numeric",
          year: "numeric",
        })}
      />

      {error && <ErrorState message={error} onRetry={reload} />}

      {loading && <DashboardSkeleton />}

      {data && !loading && (
        <>
          {/* Stat grid */}
          <Stagger className="grid grid-cols-2 gap-4 md:grid-cols-4">
            {[
              { icon: Boxes, label: "Total items", value: data.stats.totalInventories, tone: "brand" as const },
              { icon: PackageCheck, label: "Available", value: data.stats.availableInventories, tone: "success" as const },
              { icon: ClipboardList, label: "Assigned", value: data.stats.assignedInventories, tone: "info" as const },
              { icon: Users, label: "Team members", value: data.stats.totalUsers, tone: "violet" as const },
              { icon: CalendarClock, label: "Expiring soon", value: data.stats.expiringInventories, tone: "warning" as const },
              { icon: TrendingDown, label: "Low stock", value: data.stats.lowStockInventories, tone: "danger" as const },
              { icon: Clock, label: "Active assignments", value: data.stats.activeAssignments, tone: "info" as const },
              { icon: AlertTriangle, label: "Overdue", value: data.stats.overdueAssignments, tone: "danger" as const },
            ].map((s) => (
              <StaggerItem key={s.label}>
                <StatCard {...s} />
              </StaggerItem>
            ))}
          </Stagger>

          {/* Distribution + attention */}
          <div className="grid gap-4 lg:grid-cols-3">
            <FadeIn className="lg:col-span-1">
              <Card className="h-full">
                <CardHeader>
                  <CardTitle>Inventory distribution</CardTitle>
                </CardHeader>
                <CardContent className="flex flex-col items-center gap-6 py-6 sm:flex-row sm:justify-around">
                  <Donut
                    segments={[
                      { label: "Available", value: data.stats.availableInventories, color: "#12b88b" },
                      { label: "Assigned", value: data.stats.assignedInventories, color: "#38bdf8" },
                      {
                        label: "Other",
                        value: Math.max(
                          data.stats.totalInventories -
                            data.stats.availableInventories -
                            data.stats.assignedInventories,
                          0,
                        ),
                        color: "#64748b",
                      },
                    ]}
                  />
                  <div className="space-y-3">
                    {[
                      { label: "Available", value: data.stats.availableInventories, color: "bg-brand-400" },
                      { label: "Assigned", value: data.stats.assignedInventories, color: "bg-sky-400" },
                      {
                        label: "Other",
                        value: Math.max(
                          data.stats.totalInventories -
                            data.stats.availableInventories -
                            data.stats.assignedInventories,
                          0,
                        ),
                        color: "bg-slate-500",
                      },
                    ].map((l) => (
                      <div key={l.label} className="flex items-center gap-2.5">
                        <span className={cn("size-2.5 rounded-full", l.color)} />
                        <span className="text-sm text-slate-300">{l.label}</span>
                        <span className="ml-auto text-sm font-medium text-white">
                          {l.value}
                        </span>
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>
            </FadeIn>

            <FadeIn delay={0.1} className="lg:col-span-2">
              <div
                className={cn(
                  "grid h-full gap-4",
                  canManage ? "sm:grid-cols-3" : "sm:grid-cols-2",
                )}
              >
                <AlertColumn
                  icon={CalendarClock}
                  title="Expiring"
                  tone="warning"
                  count={expiring.data?.length ?? 0}
                  items={(expiring.data ?? []).slice(0, 4).map((i) => ({
                    id: i.id,
                    primary: i.name,
                    secondary:
                      i.expiryDate != null
                        ? `${describeDays(daysUntil(i.expiryDate))} · ${formatDate(i.expiryDate)}`
                        : "No expiry date",
                  }))}
                  emptyText="Nothing expiring soon"
                  control={
                    <label className="flex items-center gap-2 text-xs text-slate-400">
                      <span>Window</span>
                      <select
                        value={expiryMonths}
                        onChange={(e) => setExpiryMonths(Number(e.target.value))}
                        className="rounded-lg border border-white/10 bg-ink-800/60 px-2 py-1 text-xs text-white outline-none focus:border-brand-400/50"
                      >
                        {[3, 4, 5, 6].map((m) => (
                          <option key={m} value={m}>
                            {m} months
                          </option>
                        ))}
                      </select>
                    </label>
                  }
                />
                <AlertColumn
                  icon={TrendingDown}
                  title="Low stock"
                  tone="danger"
                  count={lowStock.data?.length ?? 0}
                  items={(lowStock.data ?? []).slice(0, 4).map((i) => ({
                    id: i.id,
                    primary: i.name,
                    secondary: `${i.availableQuantity} of ${i.quantity} available`,
                  }))}
                  emptyText="Stock levels are healthy"
                  control={
                    <label className="flex items-center gap-2 text-xs text-slate-400">
                      <span>Threshold ≤</span>
                      <input
                        type="number"
                        min={1}
                        max={999}
                        value={lowStockThreshold}
                        onChange={(e) =>
                          setLowStockThreshold(
                            Math.max(1, Number(e.target.value) || 1),
                          )
                        }
                        className="w-16 rounded-lg border border-white/10 bg-ink-800/60 px-2 py-1 text-xs text-white outline-none focus:border-brand-400/50"
                      />
                    </label>
                  }
                />
                {canManage && (
                  <AlertColumn
                    icon={ShieldAlert}
                    title="Overdue"
                    tone="danger"
                    count={data.overdue.length}
                    items={data.overdue.slice(0, 4).map((a) => ({
                      id: a.id,
                      primary: a.itemName,
                      secondary: `${a.userName} · due ${formatDate(a.expectedReturnDate)}`,
                    }))}
                    emptyText="No overdue returns"
                  />
                )}
              </div>
            </FadeIn>
          </div>

          {/* Recent activity */}
          <div className={cn("grid gap-4", canManage ? "lg:grid-cols-2" : "")}>
            <FadeIn>
              <Card>
                <CardHeader>
                  <CardTitle>Recent inventory</CardTitle>
                  <Link
                    href="/inventory"
                    className="inline-flex items-center gap-1 text-xs font-medium text-brand-300 transition hover:text-brand-200"
                  >
                    View all <ArrowUpRight className="size-3.5" />
                  </Link>
                </CardHeader>
                <CardContent className="p-0">
                  {data.recentInventories.length === 0 ? (
                    <EmptyState icon={Boxes} title="No inventory yet" />
                  ) : (
                    <ul className="divide-y divide-white/[0.05]">
                      {data.recentInventories.map((item) => (
                        <RecentInventoryRow key={item.id} item={item} />
                      ))}
                    </ul>
                  )}
                </CardContent>
              </Card>
            </FadeIn>

            {canManage && (
              <FadeIn delay={0.05}>
                <Card>
                  <CardHeader>
                    <CardTitle>Recent assignments</CardTitle>
                    <Link
                      href="/assignments"
                      className="inline-flex items-center gap-1 text-xs font-medium text-brand-300 transition hover:text-brand-200"
                    >
                      View all <ArrowUpRight className="size-3.5" />
                    </Link>
                  </CardHeader>
                  <CardContent className="p-0">
                    {data.recentAssignments.length === 0 ? (
                      <EmptyState icon={ClipboardList} title="No assignments yet" />
                    ) : (
                      <ul className="divide-y divide-white/[0.05]">
                        {data.recentAssignments.map((a) => (
                          <RecentAssignmentRow key={a.id} assignment={a} />
                        ))}
                      </ul>
                    )}
                  </CardContent>
                </Card>
              </FadeIn>
            )}
          </div>
        </>
      )}
    </div>
  );
}

function describeDays(days: number | null): string {
  if (days == null) return "—";
  if (days < 0) return `${Math.abs(days)}d overdue`;
  if (days === 0) return "Due today";
  return `in ${days}d`;
}

function AlertColumn({
  icon: Icon,
  title,
  tone,
  count,
  items,
  emptyText,
  control,
}: {
  icon: typeof CalendarClock;
  title: string;
  tone: "warning" | "danger";
  count: number;
  items: { id: number; primary: string; secondary: string }[];
  emptyText: string;
  control?: React.ReactNode;
}) {
  const toneText = tone === "warning" ? "text-amber-300" : "text-rose-300";
  const toneBg = tone === "warning" ? "bg-amber-500/10" : "bg-rose-500/10";
  return (
    <div className="panel flex flex-col rounded-2xl p-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <div className={cn("flex size-8 items-center justify-center rounded-lg", toneBg)}>
            <Icon className={cn("size-4", toneText)} />
          </div>
          <span className="text-sm font-medium text-white">{title}</span>
        </div>
        <span className={cn("text-lg font-semibold", toneText)}>{count}</span>
      </div>
      {control && <div className="mt-2.5">{control}</div>}
      <div className="mt-3 flex-1 space-y-2.5">
        {items.length === 0 ? (
          <p className="py-4 text-center text-xs text-slate-500">{emptyText}</p>
        ) : (
          items.map((it) => (
            <div key={it.id} className="min-w-0">
              <p className="truncate text-sm text-slate-200">{it.primary}</p>
              <p className="truncate text-xs text-slate-500">{it.secondary}</p>
            </div>
          ))
        )}
      </div>
    </div>
  );
}

function RecentInventoryRow({ item }: { item: InventoryDto }) {
  return (
    <li className="flex items-center gap-3 px-5 py-3 transition hover:bg-white/[0.02]">
      <div className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-white/[0.04] ring-1 ring-white/10">
        <Boxes className="size-4 text-slate-400" />
      </div>
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-medium text-white">
          {item.name}
        </p>
        <p className="truncate text-xs text-slate-500">
          {item.category ?? "Uncategorized"} · {formatRelativeTime(item.createdAt)}
        </p>
      </div>
      <span className="text-sm font-medium text-slate-300">
        {item.availableQuantity}/{item.quantity}
      </span>
    </li>
  );
}

function RecentAssignmentRow({ assignment }: { assignment: InventoryAssignmentDto }) {
  return (
    <li className="flex items-center gap-3 px-5 py-3 transition hover:bg-white/[0.02]">
      <Avatar name={assignment.userName} size="sm" />
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-medium text-white">
          {assignment.itemName}
        </p>
        <p className="truncate text-xs text-slate-500">
          {assignment.userName} · {formatRelativeTime(assignment.assignedDate)}
        </p>
      </div>
      <span className="text-sm font-medium text-slate-300">
        ×{assignment.assignedQuantity}
      </span>
    </li>
  );
}

function DashboardSkeleton() {
  return (
    <div className="space-y-8">
      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        {Array.from({ length: 8 }).map((_, i) => (
          <Skeleton key={i} className="h-[132px]" />
        ))}
      </div>
      <div className="grid gap-4 lg:grid-cols-3">
        <Skeleton className="h-64 lg:col-span-1" />
        <Skeleton className="h-64 lg:col-span-2" />
      </div>
      <div className="grid gap-4 lg:grid-cols-2">
        <Skeleton className="h-72" />
        <Skeleton className="h-72" />
      </div>
    </div>
  );
}
