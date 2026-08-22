"use client";

import { useMemo } from "react";
import {
  AlertTriangle,
  BarChart3,
  Boxes,
  CalendarClock,
  Download,
  Package,
  Printer,
  TrendingDown,
  Wallet,
} from "lucide-react";
import { api } from "@/lib/api";
import { useAsync } from "@/lib/use-async";
import { useAuth } from "@/lib/auth-context";
import {
  AssignmentStatus,
  InventoryStatus,
  type InventoryAssignmentDto,
} from "@/lib/types";
import {
  assignmentStatusLabels,
  cn,
  daysUntil,
  formatCurrency,
  formatDate,
  formatNumber,
  inventoryStatusLabels,
  inventoryStatusTones,
} from "@/lib/utils";
import { exportCsv, timestampedName } from "@/lib/export";
import { PageHeader } from "@/components/layout/page-header";
import { StatCard } from "@/components/dashboard/stat-card";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Spinner } from "@/components/ui/spinner";
import { ErrorState } from "@/components/ui/error-state";
import { FadeIn } from "@/components/ui/reveal";

const LOW_STOCK = 5;
const EXPIRY_WINDOW_DAYS = 90;

export default function ReportsPage() {
  const { canManage } = useAuth();

  const { data, loading, error, reload } = useAsync(async () => {
    const inventory = await api.inventory.list();
    let assignments: InventoryAssignmentDto[] = [];
    if (canManage) {
      try {
        assignments = await api.assignments.list();
      } catch {
        assignments = [];
      }
    }
    return { inventory, assignments };
  }, [canManage]);

  const kpis = useMemo(() => {
    const inv = data?.inventory ?? [];
    const totalUnits = inv.reduce((s, i) => s + i.quantity, 0);
    const availableUnits = inv.reduce((s, i) => s + i.availableQuantity, 0);
    const totalValue = inv.reduce(
      (s, i) => s + i.quantity * (i.purchasePrice ?? 0),
      0,
    );
    const availableValue = inv.reduce(
      (s, i) => s + i.availableQuantity * (i.purchasePrice ?? 0),
      0,
    );
    const lowStock = inv.filter((i) => i.availableQuantity <= LOW_STOCK).length;
    const expiring = inv.filter((i) => {
      const d = daysUntil(i.expiryDate);
      return d != null && d >= 0 && d <= EXPIRY_WINDOW_DAYS;
    }).length;

    const catMap = new Map<string, { count: number; units: number; value: number }>();
    for (const i of inv) {
      const key = i.category?.trim() || "Uncategorized";
      const cur = catMap.get(key) ?? { count: 0, units: 0, value: 0 };
      cur.count += 1;
      cur.units += i.quantity;
      cur.value += i.quantity * (i.purchasePrice ?? 0);
      catMap.set(key, cur);
    }
    const categories = Array.from(catMap.entries())
      .map(([name, v]) => ({ name, ...v }))
      .sort((a, b) => b.value - a.value);
    const maxCatValue = Math.max(1, ...categories.map((c) => c.value));

    const statusCounts = new Map<InventoryStatus, number>();
    for (const i of inv)
      statusCounts.set(i.status, (statusCounts.get(i.status) ?? 0) + 1);

    return {
      totalItems: inv.length,
      totalUnits,
      availableUnits,
      totalValue,
      availableValue,
      lowStock,
      expiring,
      categories,
      maxCatValue,
      statusCounts,
    };
  }, [data]);

  const assignmentKpis = useMemo(() => {
    const a = data?.assignments ?? [];
    const active = a.filter((x) => x.status === AssignmentStatus.Active);
    const overdue = active.filter((x) => {
      const d = daysUntil(x.expectedReturnDate);
      return d != null && d < 0;
    });
    const assignedUnits = Math.max(kpis.totalUnits - kpis.availableUnits, 0);
    const utilization =
      kpis.totalUnits > 0 ? Math.round((assignedUnits / kpis.totalUnits) * 100) : 0;

    const byItem = new Map<string, number>();
    for (const x of a) byItem.set(x.itemName, (byItem.get(x.itemName) ?? 0) + 1);
    const topItems = Array.from(byItem.entries())
      .map(([name, count]) => ({ name, count }))
      .sort((x, y) => y.count - x.count)
      .slice(0, 5);

    return {
      total: a.length,
      active: active.length,
      overdue: overdue.length,
      utilization,
      topItems,
    };
  }, [data, kpis]);

  function exportInventory() {
    exportCsv(timestampedName("inventory"), data?.inventory ?? [], [
      { header: "ID", value: (i) => i.id },
      { header: "Item", value: (i) => i.name },
      { header: "Category", value: (i) => i.category },
      { header: "Brand", value: (i) => i.brand },
      { header: "Model", value: (i) => i.model },
      { header: "Serial", value: (i) => i.serialNumber },
      { header: "Barcode", value: (i) => i.barcode },
      { header: "Quantity", value: (i) => i.quantity },
      { header: "Available", value: (i) => i.availableQuantity },
      { header: "Status", value: (i) => inventoryStatusLabels[i.status] },
      { header: "Purchase price", value: (i) => i.purchasePrice ?? "" },
      { header: "Supplier", value: (i) => i.supplier },
      { header: "Location", value: (i) => i.location },
      { header: "Expiry", value: (i) => (i.expiryDate ? formatDate(i.expiryDate) : "") },
      { header: "Created", value: (i) => formatDate(i.createdAt) },
    ]);
  }

  function exportAssignments() {
    exportCsv(timestampedName("assignments"), data?.assignments ?? [], [
      { header: "ID", value: (a) => a.id },
      { header: "Item", value: (a) => a.itemName },
      { header: "Assignee", value: (a) => a.userName },
      { header: "Email", value: (a) => a.userEmail },
      { header: "Quantity", value: (a) => a.assignedQuantity },
      { header: "Status", value: (a) => assignmentStatusLabels[a.status] },
      { header: "Assigned", value: (a) => formatDate(a.assignedDate) },
      {
        header: "Expected return",
        value: (a) => (a.expectedReturnDate ? formatDate(a.expectedReturnDate) : ""),
      },
      { header: "Returned", value: (a) => (a.returnDate ? formatDate(a.returnDate) : "") },
    ]);
  }

  return (
    <div className="space-y-6">
      <PageHeader
        icon={BarChart3}
        title="Reports"
        description="Inventory value, utilization and export."
        actions={
          <div className="flex items-center gap-2.5 print:hidden">
            <Button
              variant="secondary"
              size="sm"
              onClick={exportInventory}
              leftIcon={<Download className="size-4" />}
            >
              Inventory CSV
            </Button>
            {canManage && (
              <Button
                variant="secondary"
                size="sm"
                onClick={exportAssignments}
                leftIcon={<Download className="size-4" />}
              >
                Assignments CSV
              </Button>
            )}
            <Button
              size="sm"
              onClick={() => window.print()}
              leftIcon={<Printer className="size-4" />}
            >
              Print
            </Button>
          </div>
        }
      />

      {error ? (
        <ErrorState message={error} onRetry={reload} />
      ) : loading || !data ? (
        <div className="flex justify-center py-24">
          <Spinner className="size-7" />
        </div>
      ) : (
        <FadeIn className="space-y-6">
          <div className="grid grid-cols-2 gap-4 md:grid-cols-3 xl:grid-cols-6">
            <StatCard icon={Boxes} label="Distinct items" value={kpis.totalItems} tone="brand" />
            <StatCard icon={Package} label="Total units" value={kpis.totalUnits} tone="info" />
            <StatCard
              icon={Wallet}
              label="Total stock value"
              value={kpis.totalValue}
              tone="success"
              format={formatCurrency}
            />
            <StatCard
              icon={Wallet}
              label="Available value"
              value={kpis.availableValue}
              tone="brand"
              format={formatCurrency}
            />
            <StatCard icon={TrendingDown} label="Low stock" value={kpis.lowStock} tone="danger" />
            <StatCard icon={CalendarClock} label="Expiring ≤90d" value={kpis.expiring} tone="warning" />
          </div>

          <div className="grid gap-4 lg:grid-cols-3">
            <Card className="lg:col-span-2">
              <CardHeader>
                <CardTitle>Value by category</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                {kpis.categories.length === 0 ? (
                  <p className="py-4 text-sm text-slate-500">No inventory to report.</p>
                ) : (
                  kpis.categories.map((c) => (
                    <div key={c.name}>
                      <div className="mb-1 flex items-center justify-between text-sm">
                        <span className="text-slate-200">{c.name}</span>
                        <span className="text-slate-400">
                          {formatCurrency(c.value)} · {c.count} item{c.count === 1 ? "" : "s"}
                        </span>
                      </div>
                      <div className="h-2 overflow-hidden rounded-full bg-white/[0.06]">
                        <div
                          className="h-full rounded-full bg-gradient-to-r from-brand-400 to-brand-500"
                          style={{ width: `${Math.round((c.value / kpis.maxCatValue) * 100)}%` }}
                        />
                      </div>
                    </div>
                  ))
                )}
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Status breakdown</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2.5">
                {Object.values(InventoryStatus)
                  .filter((v): v is number => typeof v === "number")
                  .map((s) => (
                    <div key={s} className="flex items-center justify-between">
                      <Badge tone={inventoryStatusTones[s as InventoryStatus]} dot>
                        {inventoryStatusLabels[s as InventoryStatus]}
                      </Badge>
                      <span className="text-sm font-medium text-white">
                        {formatNumber(kpis.statusCounts.get(s as InventoryStatus) ?? 0)}
                      </span>
                    </div>
                  ))}
              </CardContent>
            </Card>
          </div>

          {canManage && (
            <div className="grid gap-4 lg:grid-cols-3">
              <div className="grid grid-cols-3 gap-4 lg:col-span-1 lg:grid-cols-1">
                <StatCard icon={BarChart3} label="Utilization" value={assignmentKpis.utilization} tone="violet" format={(n) => `${Math.round(n)}%`} />
                <StatCard icon={AlertTriangle} label="Overdue" value={assignmentKpis.overdue} tone="danger" />
                <StatCard icon={Package} label="Active loans" value={assignmentKpis.active} tone="info" />
              </div>
              <Card className="lg:col-span-2">
                <CardHeader>
                  <CardTitle>Most-assigned items</CardTitle>
                </CardHeader>
                <CardContent className="space-y-2.5">
                  {assignmentKpis.topItems.length === 0 ? (
                    <p className="py-4 text-sm text-slate-500">No assignments recorded.</p>
                  ) : (
                    assignmentKpis.topItems.map((t) => (
                      <div
                        key={t.name}
                        className="flex items-center justify-between rounded-lg border border-white/[0.05] bg-white/[0.02] px-3 py-2"
                      >
                        <span className="truncate text-sm text-slate-200">{t.name}</span>
                        <span className={cn("text-sm font-semibold text-brand-300")}>
                          ×{t.count}
                        </span>
                      </div>
                    ))
                  )}
                </CardContent>
              </Card>
            </div>
          )}
        </FadeIn>
      )}
    </div>
  );
}
