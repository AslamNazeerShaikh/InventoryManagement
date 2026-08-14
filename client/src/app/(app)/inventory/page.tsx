"use client";

import { useEffect, useMemo, useState } from "react";
import {
  Boxes,
  Eye,
  Pencil,
  Plus,
  RefreshCw,
  Search,
  Trash2,
} from "lucide-react";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import { useAsync } from "@/lib/use-async";
import { useAuth } from "@/lib/auth-context";
import { InventoryStatus, type InventoryDto } from "@/lib/types";
import {
  cn,
  daysUntil,
  formatDate,
  inventoryStatusLabels,
} from "@/lib/utils";
import { PageHeader } from "@/components/layout/page-header";
import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { ErrorState } from "@/components/ui/error-state";
import { Pagination } from "@/components/ui/pagination";
import { Table, TBody, TD, TH, THead, TR } from "@/components/ui/table";
import { InventoryStatusBadge } from "@/components/domain/status-badges";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { InventoryFormModal } from "@/components/inventory/inventory-form-modal";
import { InventoryDetailsModal } from "@/components/inventory/inventory-details-modal";
import { FadeIn } from "@/components/ui/reveal";

const PAGE_SIZE = 8;

export default function InventoryPage() {
  const { canManage, isAdmin } = useAuth();
  const { data, loading, error, reload } = useAsync(
    () => api.inventory.list(),
    [],
  );

  const [search, setSearch] = useState("");
  const [category, setCategory] = useState("all");
  const [status, setStatus] = useState("all");
  const [page, setPage] = useState(1);

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<InventoryDto | null>(null);
  const [details, setDetails] = useState<InventoryDto | null>(null);
  const [toDelete, setToDelete] = useState<InventoryDto | null>(null);
  const [deleting, setDeleting] = useState(false);

  const categories = useMemo(() => {
    const set = new Set<string>();
    (data ?? []).forEach((i) => i.category && set.add(i.category));
    return Array.from(set).sort();
  }, [data]);

  const filtered = useMemo(() => {
    let list = data ?? [];
    const term = search.trim().toLowerCase();
    if (term) {
      list = list.filter((i) =>
        [i.equipmentName, i.barcode, i.serialNumber, i.brand, i.category, i.model]
          .filter(Boolean)
          .some((f) => f!.toLowerCase().includes(term)),
      );
    }
    if (category !== "all") list = list.filter((i) => (i.category ?? "") === category);
    if (status !== "all") list = list.filter((i) => i.status === Number(status));
    return list;
  }, [data, search, category, status]);

  useEffect(() => setPage(1), [search, category, status]);

  const paged = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  async function handleDelete() {
    if (!toDelete) return;
    setDeleting(true);
    try {
      await api.inventory.remove(toDelete.id);
      toast.success("Item deleted", {
        description: `${toDelete.equipmentName} was removed.`,
      });
      setToDelete(null);
      reload();
    } catch (err) {
      toast.error("Delete failed", {
        description: err instanceof ApiError ? err.message : undefined,
      });
    } finally {
      setDeleting(false);
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader
        icon={Boxes}
        title="Inventory"
        description="Medical equipment catalogue and stock levels."
        actions={
          <>
            <Button
              variant="secondary"
              size="icon"
              onClick={() => reload()}
              aria-label="Refresh"
            >
              <RefreshCw className={cn("size-4", loading && "animate-spin")} />
            </Button>
            {canManage && (
              <Button
                onClick={() => {
                  setEditing(null);
                  setFormOpen(true);
                }}
                leftIcon={<Plus className="size-4" />}
              >
                Add item
              </Button>
            )}
          </>
        }
      />

      <Card>
        {/* Toolbar */}
        <div className="flex flex-col gap-3 border-b border-white/[0.06] p-4 sm:flex-row sm:items-center">
          <div className="relative flex-1">
            <Search className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-slate-500" />
            <Input
              placeholder="Search by name, barcode, serial, brand…"
              className="pl-10"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          <div className="flex gap-3">
            <Select
              className="min-w-[9rem]"
              value={category}
              onChange={(e) => setCategory(e.target.value)}
            >
              <option value="all">All categories</option>
              {categories.map((c) => (
                <option key={c} value={c}>
                  {c}
                </option>
              ))}
            </Select>
            <Select
              className="min-w-[9rem]"
              value={status}
              onChange={(e) => setStatus(e.target.value)}
            >
              <option value="all">All statuses</option>
              {Object.values(InventoryStatus)
                .filter((v): v is number => typeof v === "number")
                .map((v) => (
                  <option key={v} value={v}>
                    {inventoryStatusLabels[v as InventoryStatus]}
                  </option>
                ))}
            </Select>
          </div>
        </div>

        {error ? (
          <ErrorState message={error} onRetry={reload} />
        ) : loading ? (
          <div className="space-y-2 p-4">
            {Array.from({ length: 6 }).map((_, i) => (
              <Skeleton key={i} className="h-14" />
            ))}
          </div>
        ) : filtered.length === 0 ? (
          <EmptyState
            icon={Boxes}
            title={data && data.length > 0 ? "No matching items" : "No inventory yet"}
            description={
              data && data.length > 0
                ? "Try adjusting your search or filters."
                : "Add your first piece of equipment to get started."
            }
            action={
              canManage && (!data || data.length === 0) ? (
                <Button
                  onClick={() => {
                    setEditing(null);
                    setFormOpen(true);
                  }}
                  leftIcon={<Plus className="size-4" />}
                >
                  Add item
                </Button>
              ) : undefined
            }
          />
        ) : (
          <FadeIn>
            <Table>
              <THead>
                <TR className="hover:bg-transparent">
                  <TH>Item</TH>
                  <TH className="hidden md:table-cell">Barcode</TH>
                  <TH>Stock</TH>
                  <TH>Status</TH>
                  <TH className="hidden lg:table-cell">Expiry</TH>
                  <TH className="text-right">Actions</TH>
                </TR>
              </THead>
              <TBody>
                {paged.map((item) => {
                  const days = daysUntil(item.expiryDate);
                  const expiryTone =
                    days == null
                      ? "text-slate-400"
                      : days < 0
                        ? "text-rose-300"
                        : days <= 90
                          ? "text-amber-300"
                          : "text-slate-300";
                  return (
                    <TR key={item.id}>
                      <TD>
                        <div className="flex items-center gap-3">
                          <div className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-white/[0.04] ring-1 ring-white/10">
                            <Boxes className="size-4 text-slate-400" />
                          </div>
                          <div className="min-w-0">
                            <p className="truncate font-medium text-white">
                              {item.equipmentName}
                            </p>
                            <p className="truncate text-xs text-slate-500">
                              {[item.category, item.brand]
                                .filter(Boolean)
                                .join(" · ") || "—"}
                            </p>
                          </div>
                        </div>
                      </TD>
                      <TD className="hidden font-mono text-xs text-slate-400 md:table-cell">
                        {item.barcode ?? "—"}
                      </TD>
                      <TD>
                        <div className="w-28">
                          <div className="mb-1 flex justify-between text-xs text-slate-400">
                            <span className="font-medium text-slate-200">
                              {item.availableQuantity}
                            </span>
                            <span>/ {item.quantity}</span>
                          </div>
                          <div className="h-1.5 overflow-hidden rounded-full bg-white/[0.06]">
                            <div
                              className="h-full rounded-full bg-gradient-to-r from-brand-400 to-brand-500"
                              style={{
                                width: `${
                                  item.quantity > 0
                                    ? Math.round(
                                        (item.availableQuantity / item.quantity) *
                                          100,
                                      )
                                    : 0
                                }%`,
                              }}
                            />
                          </div>
                        </div>
                      </TD>
                      <TD>
                        <InventoryStatusBadge status={item.status} />
                      </TD>
                      <TD className={cn("hidden lg:table-cell", expiryTone)}>
                        {formatDate(item.expiryDate)}
                      </TD>
                      <TD>
                        <div className="flex items-center justify-end gap-1">
                          <IconAction
                            label="View"
                            onClick={() => setDetails(item)}
                          >
                            <Eye className="size-4" />
                          </IconAction>
                          {canManage && (
                            <IconAction
                              label="Edit"
                              onClick={() => {
                                setEditing(item);
                                setFormOpen(true);
                              }}
                            >
                              <Pencil className="size-4" />
                            </IconAction>
                          )}
                          {isAdmin && (
                            <IconAction
                              label="Delete"
                              danger
                              onClick={() => setToDelete(item)}
                            >
                              <Trash2 className="size-4" />
                            </IconAction>
                          )}
                        </div>
                      </TD>
                    </TR>
                  );
                })}
              </TBody>
            </Table>
            <Pagination
              page={page}
              pageSize={PAGE_SIZE}
              total={filtered.length}
              onPage={setPage}
            />
          </FadeIn>
        )}
      </Card>

      <InventoryFormModal
        open={formOpen}
        onClose={() => setFormOpen(false)}
        initial={editing}
        onSaved={reload}
      />
      <InventoryDetailsModal
        open={Boolean(details)}
        onClose={() => setDetails(null)}
        item={details}
        canViewHistory={canManage}
      />
      <ConfirmDialog
        open={Boolean(toDelete)}
        onClose={() => setToDelete(null)}
        onConfirm={handleDelete}
        loading={deleting}
        title="Delete inventory item"
        description={`This will permanently remove "${toDelete?.equipmentName}". This action cannot be undone.`}
        confirmText="Delete"
      />
    </div>
  );
}

function IconAction({
  label,
  onClick,
  danger = false,
  children,
}: {
  label: string;
  onClick: () => void;
  danger?: boolean;
  children: React.ReactNode;
}) {
  return (
    <button
      onClick={onClick}
      title={label}
      aria-label={label}
      className={cn(
        "flex size-8 items-center justify-center rounded-lg text-slate-400 transition hover:bg-white/[0.06]",
        danger ? "hover:text-rose-300" : "hover:text-white",
      )}
    >
      {children}
    </button>
  );
}
