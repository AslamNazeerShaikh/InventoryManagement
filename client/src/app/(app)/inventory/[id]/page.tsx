"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import {
  ArrowLeft,
  ArrowLeftRight,
  Boxes,
  History,
  PackagePlus,
  Pencil,
  Plus,
  SlidersHorizontal,
  Trash2,
  Wrench,
} from "lucide-react";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import { useAsync } from "@/lib/use-async";
import { useAuth } from "@/lib/auth-context";
import type {
  AssignmentHistoryDto,
  MaintenanceScheduleDto,
  StockMovementDto,
} from "@/lib/types";
import {
  formatCurrency,
  formatDate,
  formatDateTime,
  maintenanceTypeLabels,
  stockMovementLabels,
} from "@/lib/utils";
import { PageHeader } from "@/components/layout/page-header";
import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Spinner } from "@/components/ui/spinner";
import { ErrorState } from "@/components/ui/error-state";
import {
  AssignmentStatusBadge,
  InventoryStatusBadge,
  MaintenanceStatusBadge,
  StockMovementBadge,
} from "@/components/domain/status-badges";
import { InventoryFormModal } from "@/components/inventory/inventory-form-modal";
import {
  StockActionModal,
  type StockAction,
} from "@/components/inventory/stock-action-modal";
import { MaintenanceFormModal } from "@/components/maintenance/maintenance-form-modal";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { FadeIn } from "@/components/ui/reveal";

function Detail({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="min-w-0">
      <dt className="text-xs uppercase tracking-wide text-slate-500">{label}</dt>
      <dd className="mt-0.5 text-sm text-slate-200">{value ?? "—"}</dd>
    </div>
  );
}

export default function InventoryDetailPage() {
  const params = useParams<{ id: string }>();
  const id = Number(params.id);
  const router = useRouter();
  const { canManage, isAdmin } = useAuth();

  const [editing, setEditing] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [stockAction, setStockAction] = useState<StockAction | null>(null);
  const [addingSchedule, setAddingSchedule] = useState(false);

  const { data, loading, error, reload } = useAsync(async () => {
    const item = await api.inventory.byId(id);
    let history: AssignmentHistoryDto | null = null;
    let movements: StockMovementDto[] = [];
    let maintenance: MaintenanceScheduleDto[] = [];
    if (canManage) {
      [history, movements, maintenance] = await Promise.all([
        api.assignments.history(id).catch(() => null),
        api.inventory.movements(id).catch(() => [] as StockMovementDto[]),
        api.maintenance
          .byInventory(id)
          .catch(() => [] as MaintenanceScheduleDto[]),
      ]);
    }
    return { item, history, movements, maintenance };
  }, [id, canManage]);

  async function handleDelete() {
    setDeleting(true);
    try {
      await api.inventory.remove(id);
      toast.success("Item deleted");
      router.push("/inventory");
    } catch (err) {
      toast.error("Delete failed", {
        description: err instanceof ApiError ? err.message : undefined,
      });
      setDeleting(false);
    }
  }

  const item = data?.item;
  const pct =
    item && item.quantity > 0
      ? Math.round((item.availableQuantity / item.quantity) * 100)
      : 0;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-3">
        <Link
          href="/inventory"
          className="inline-flex items-center gap-2 text-sm text-slate-400 transition hover:text-white"
        >
          <ArrowLeft className="size-4" /> Back to inventory
        </Link>
        {item && canManage && (
          <div className="flex items-center gap-2.5">
            <Button
              variant="secondary"
              size="sm"
              onClick={() => setEditing(true)}
              leftIcon={<Pencil className="size-4" />}
            >
              Edit
            </Button>
            {isAdmin && (
              <Button
                variant="danger"
                size="sm"
                onClick={() => setConfirmDelete(true)}
                leftIcon={<Trash2 className="size-4" />}
              >
                Delete
              </Button>
            )}
          </div>
        )}
      </div>

      {error ? (
        <ErrorState message={error} onRetry={reload} />
      ) : loading || !item ? (
        <div className="flex justify-center py-24">
          <Spinner className="size-7" />
        </div>
      ) : (
        <FadeIn className="space-y-6">
          <PageHeader
            icon={Boxes}
            title={item.equipmentName}
            description={
              [item.category, item.brand].filter(Boolean).join(" · ") ||
              "Inventory item"
            }
            actions={
              <div className="flex items-center gap-2">
                {item.needsReorder && (
                  <Badge tone="warning" dot>
                    Reorder
                  </Badge>
                )}
                <InventoryStatusBadge status={item.status} />
              </div>
            }
          />

          {canManage && (
            <div className="flex flex-wrap gap-2">
              <Button
                variant="secondary"
                size="sm"
                onClick={() => setStockAction("receive")}
                leftIcon={<PackagePlus className="size-4" />}
              >
                Receive
              </Button>
              <Button
                variant="secondary"
                size="sm"
                onClick={() => setStockAction("adjust")}
                leftIcon={<SlidersHorizontal className="size-4" />}
              >
                Adjust
              </Button>
              <Button
                variant="secondary"
                size="sm"
                onClick={() => setStockAction("transfer")}
                leftIcon={<ArrowLeftRight className="size-4" />}
              >
                Transfer
              </Button>
              <Button
                variant="secondary"
                size="sm"
                onClick={() => setStockAction("dispose")}
                leftIcon={<Trash2 className="size-4" />}
              >
                Dispose
              </Button>
            </div>
          )}

          <div className="grid gap-6 lg:grid-cols-3">
            <Card className="lg:col-span-2">
              <div className="space-y-6 p-5">
                <div className="rounded-xl border border-white/[0.06] bg-white/[0.02] p-4">
                  <div className="mb-2 flex items-center justify-between text-sm">
                    <span className="text-slate-400">Available stock</span>
                    <span className="font-medium text-white">
                      {item.availableQuantity} / {item.quantity}
                    </span>
                  </div>
                  <div className="h-2 overflow-hidden rounded-full bg-white/[0.06]">
                    <div
                      className="h-full rounded-full bg-gradient-to-r from-brand-400 to-brand-500"
                      style={{ width: `${pct}%` }}
                    />
                  </div>
                  {(item.reorderLevel != null ||
                    item.reorderQuantity != null) && (
                    <p className="mt-2 text-xs text-slate-500">
                      {item.reorderLevel != null &&
                        `Reorder at ${item.reorderLevel}`}
                      {item.reorderLevel != null &&
                        item.reorderQuantity != null &&
                        " · "}
                      {item.reorderQuantity != null &&
                        `Suggested order ${item.reorderQuantity}`}
                    </p>
                  )}
                </div>

                <dl className="grid grid-cols-2 gap-4 sm:grid-cols-3">
                  <Detail
                    label="Barcode"
                    value={
                      item.barcode ? (
                        <span className="font-mono">{item.barcode}</span>
                      ) : null
                    }
                  />
                  <Detail label="Serial" value={item.serialNumber} />
                  <Detail label="Brand" value={item.brand} />
                  <Detail label="Model" value={item.model} />
                  <Detail
                    label="Supplier"
                    value={item.supplierName ?? item.supplier}
                  />
                  <Detail
                    label="Location"
                    value={item.locationName ?? item.location}
                  />
                  <Detail
                    label="Purchase price"
                    value={formatCurrency(item.purchasePrice)}
                  />
                  <Detail label="Expiry" value={formatDate(item.expiryDate)} />
                  <Detail
                    label="Manufactured"
                    value={formatDate(item.manufactureDate)}
                  />
                  <Detail label="Created" value={formatDate(item.createdAt)} />
                  <Detail label="Created by" value={item.createdByUserName} />
                </dl>

                {(item.description || item.notes) && (
                  <div className="space-y-3 border-t border-white/[0.06] pt-4">
                    {item.description && (
                      <div>
                        <p className="text-xs uppercase tracking-wide text-slate-500">
                          Description
                        </p>
                        <p className="mt-1 text-sm text-slate-300">
                          {item.description}
                        </p>
                      </div>
                    )}
                    {item.notes && (
                      <div>
                        <p className="text-xs uppercase tracking-wide text-slate-500">
                          Notes
                        </p>
                        <p className="mt-1 text-sm text-slate-300">
                          {item.notes}
                        </p>
                      </div>
                    )}
                  </div>
                )}
              </div>
            </Card>

            <Card className="lg:col-span-1">
              <div className="border-b border-white/[0.06] px-5 py-4">
                <div className="flex items-center gap-2">
                  <History className="size-4 text-slate-400" />
                  <p className="text-sm font-medium text-white">
                    Assignment history
                  </p>
                </div>
              </div>
              <div className="p-5">
                {!canManage ? (
                  <p className="text-sm text-slate-500">
                    You do not have permission to view assignment history.
                  </p>
                ) : !data?.history || data.history.assignments.length === 0 ? (
                  <p className="text-sm text-slate-500">
                    No assignment history yet.
                  </p>
                ) : (
                  <ul className="space-y-2">
                    {data.history.assignments.map((a) => (
                      <li
                        key={a.id}
                        className="rounded-lg border border-white/[0.05] bg-white/[0.02] px-3 py-2"
                      >
                        <div className="flex items-center justify-between gap-2">
                          <p className="truncate text-sm text-slate-200">
                            {a.userName} · ×{a.assignedQuantity}
                          </p>
                          <AssignmentStatusBadge status={a.status} />
                        </div>
                        <p className="mt-0.5 truncate text-xs text-slate-500">
                          {formatDateTime(a.assignedDate)}
                          {a.returnDate
                            ? ` · returned ${formatDate(a.returnDate)}`
                            : ""}
                        </p>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </Card>
          </div>

          {canManage && (
            <div className="grid gap-6 lg:grid-cols-2">
              <Card>
                <div className="border-b border-white/[0.06] px-5 py-4">
                  <div className="flex items-center gap-2">
                    <History className="size-4 text-slate-400" />
                    <p className="text-sm font-medium text-white">
                      Stock movements
                    </p>
                  </div>
                </div>
                <div className="p-5">
                  {!data?.movements || data.movements.length === 0 ? (
                    <p className="text-sm text-slate-500">No movements yet.</p>
                  ) : (
                    <ul className="space-y-2">
                      {data.movements.map((m) => (
                        <li
                          key={m.id}
                          className="rounded-lg border border-white/[0.05] bg-white/[0.02] px-3 py-2"
                        >
                          <div className="flex items-center justify-between gap-2">
                            <StockMovementBadge type={m.movementType} />
                            <span
                              className={
                                m.quantityChange > 0
                                  ? "text-sm font-medium text-emerald-300"
                                  : m.quantityChange < 0
                                    ? "text-sm font-medium text-rose-300"
                                    : "text-sm font-medium text-slate-400"
                              }
                            >
                              {m.quantityChange > 0 ? "+" : ""}
                              {m.quantityChange}
                            </span>
                          </div>
                          <p className="mt-1 flex items-center justify-between gap-2 text-xs text-slate-500">
                            <span className="truncate">
                              {m.reason ?? stockMovementLabels[m.movementType]}
                              {m.performedByUserName
                                ? ` · ${m.performedByUserName}`
                                : ""}
                            </span>
                            <span className="shrink-0">bal {m.balanceAfter}</span>
                          </p>
                          <p className="mt-0.5 text-[11px] text-slate-600">
                            {formatDateTime(m.createdAt)}
                          </p>
                        </li>
                      ))}
                    </ul>
                  )}
                </div>
              </Card>

              <Card>
                <div className="flex items-center justify-between border-b border-white/[0.06] px-5 py-4">
                  <div className="flex items-center gap-2">
                    <Wrench className="size-4 text-slate-400" />
                    <p className="text-sm font-medium text-white">Maintenance</p>
                  </div>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setAddingSchedule(true)}
                    leftIcon={<Plus className="size-4" />}
                  >
                    Schedule
                  </Button>
                </div>
                <div className="p-5">
                  {!data?.maintenance || data.maintenance.length === 0 ? (
                    <p className="text-sm text-slate-500">
                      No maintenance scheduled.
                    </p>
                  ) : (
                    <ul className="space-y-2">
                      {data.maintenance.map((m) => (
                        <li
                          key={m.id}
                          className="rounded-lg border border-white/[0.05] bg-white/[0.02] px-3 py-2"
                        >
                          <div className="flex items-center justify-between gap-2">
                            <p className="truncate text-sm text-slate-200">
                              {m.title}
                            </p>
                            <MaintenanceStatusBadge status={m.status} />
                          </div>
                          <p className="mt-0.5 truncate text-xs text-slate-500">
                            {maintenanceTypeLabels[m.maintenanceType]} · due{" "}
                            {formatDate(m.nextDueAt)}
                          </p>
                        </li>
                      ))}
                    </ul>
                  )}
                </div>
              </Card>
            </div>
          )}
        </FadeIn>
      )}

      <InventoryFormModal
        open={editing}
        onClose={() => setEditing(false)}
        initial={item ?? null}
        onSaved={reload}
      />
      {item && (
        <>
          <StockActionModal
            open={stockAction !== null}
            onClose={() => setStockAction(null)}
            action={stockAction ?? "receive"}
            item={item}
            onSaved={reload}
          />
          <MaintenanceFormModal
            open={addingSchedule}
            onClose={() => setAddingSchedule(false)}
            presetInventoryId={item.id}
            onSaved={reload}
          />
        </>
      )}
      <ConfirmDialog
        open={confirmDelete}
        onClose={() => setConfirmDelete(false)}
        onConfirm={handleDelete}
        loading={deleting}
        title="Delete inventory item"
        description={`This will permanently remove "${item?.equipmentName}". This action cannot be undone.`}
        confirmText="Delete"
      />
    </div>
  );
}
