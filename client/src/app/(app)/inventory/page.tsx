"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import {
  Boxes,
  Eye,
  Filter,
  Pencil,
  Plus,
  RefreshCw,
  Search,
  Trash2,
  X,
} from "lucide-react";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import { useAsync } from "@/lib/use-async";
import { useDebouncedValue } from "@/lib/use-debounce";
import { useAuth } from "@/lib/auth-context";
import { InventoryStatus, type InventoryDto } from "@/lib/types";
import {
  cn,
  dateInputToIso,
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
import { Table, TBody, TD, TH, THead, TR } from "@/components/ui/table";
import { InventoryStatusBadge } from "@/components/domain/status-badges";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { InventoryFormModal } from "@/components/inventory/inventory-form-modal";
import { InventoryDetailsModal } from "@/components/inventory/inventory-details-modal";
import { FadeIn } from "@/components/ui/reveal";

const PAGE_SIZE = 8;

export default function InventoryPage() {
  const { canManage, isAdmin } = useAuth();

  // Text search is debounced so we hit the server once per pause, not per keystroke.
  const [searchInput, setSearchInput] = useState("");
  const searchTerm = useDebouncedValue(searchInput, 350);
  const [category, setCategory] = useState("");
  const [status, setStatus] = useState("all");
  const [expiryFrom, setExpiryFrom] = useState("");
  const [expiryTo, setExpiryTo] = useState("");
  const [showFilters, setShowFilters] = useState(false);
  const [page, setPage] = useState(1);

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<InventoryDto | null>(null);
  const [details, setDetails] = useState<InventoryDto | null>(null);
  const [toDelete, setToDelete] = useState<InventoryDto | null>(null);
  const [deleting, setDeleting] = useState(false);

  const hasCriteria = Boolean(
    searchTerm.trim() ||
      category.trim() ||
      status !== "all" ||
      expiryFrom ||
      expiryTo,
  );

  // Reset to the first page whenever the criteria change.
  useEffect(() => {
    setPage(1);
  }, [searchTerm, category, status, expiryFrom, expiryTo]);

  // Server-side data: filtered search (POST /search) when any criteria are set,
  // otherwise a plain page (GET /paged) that also returns an accurate total.
  const { data, loading, error, reload } = useAsync(async () => {
    if (hasCriteria) {
      const results = await api.inventory.search({
        searchTerm: searchTerm.trim() || null,
        category: category.trim() || null,
        status: status === "all" ? null : (Number(status) as InventoryStatus),
        expiryDateFrom: dateInputToIso(expiryFrom),
        expiryDateTo: dateInputToIso(expiryTo),
        pageNumber: page,
        pageSize: PAGE_SIZE,
      });
      // /search returns a page of items without a total, so infer "has next".
      return {
        items: results,
        total: null as number | null,
        hasNext: results.length === PAGE_SIZE,
      };
    }
    const result = await api.inventory.paged(page, PAGE_SIZE);
    return {
      items: result.data,
      total: result.totalCount,
      hasNext: result.hasNextPage,
    };
  }, [hasCriteria, searchTerm, category, status, expiryFrom, expiryTo, page]);

  const items = data?.items ?? [];

  function clearFilters() {
    setSearchInput("");
    setCategory("");
    setStatus("all");
    setExpiryFrom("");
    setExpiryTo("");
  }

  async function handleDelete() {
    if (!toDelete) return;
    setDeleting(true);
    try {
      await api.inventory.remove(toDelete.id);
      toast.success("Item deleted", {
        description: `${toDelete.name} was removed.`,
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
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
            />
          </div>
          <div className="flex gap-3">
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
            <Button
              variant={showFilters ? "subtle" : "secondary"}
              onClick={() => setShowFilters((s) => !s)}
              leftIcon={<Filter className="size-4" />}
            >
              Filters
            </Button>
          </div>
        </div>

        {showFilters && (
          <div className="grid gap-3 border-b border-white/[0.06] p-4 sm:grid-cols-2 lg:grid-cols-4">
            <div>
              <label className="mb-1 block text-xs text-slate-400">Category</label>
              <Input
                placeholder="e.g. Diagnostic Equipment"
                value={category}
                onChange={(e) => setCategory(e.target.value)}
              />
            </div>
            <div>
              <label className="mb-1 block text-xs text-slate-400">Expiry from</label>
              <Input
                type="date"
                value={expiryFrom}
                onChange={(e) => setExpiryFrom(e.target.value)}
              />
            </div>
            <div>
              <label className="mb-1 block text-xs text-slate-400">Expiry to</label>
              <Input
                type="date"
                value={expiryTo}
                onChange={(e) => setExpiryTo(e.target.value)}
              />
            </div>
            <div className="flex items-end">
              <Button
                variant="ghost"
                onClick={clearFilters}
                leftIcon={<X className="size-4" />}
                disabled={!hasCriteria}
              >
                Clear
              </Button>
            </div>
          </div>
        )}

        {error ? (
          <ErrorState message={error} onRetry={reload} />
        ) : loading && !data ? (
          <div className="space-y-2 p-4">
            {Array.from({ length: 6 }).map((_, i) => (
              <Skeleton key={i} className="h-14" />
            ))}
          </div>
        ) : items.length === 0 ? (
          <EmptyState
            icon={Boxes}
            title={hasCriteria ? "No matching items" : "No inventory yet"}
            description={
              hasCriteria
                ? "Try adjusting your search or filters."
                : "Add your first piece of equipment to get started."
            }
            action={
              canManage && !hasCriteria ? (
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
                {items.map((item) => {
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
                            <Link
                              href={`/inventory/${item.id}`}
                              className="block truncate font-medium text-white transition hover:text-brand-300"
                            >
                              {item.name}
                            </Link>
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
            <div className="flex items-center justify-between gap-3 border-t border-white/[0.06] px-4 py-3 text-sm">
              <p className="text-slate-400">
                {data?.total != null
                  ? `Page ${page} · ${data.total} item${data.total === 1 ? "" : "s"} total`
                  : `Page ${page}`}
              </p>
              <div className="flex items-center gap-1.5">
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={page <= 1 || loading}
                >
                  Previous
                </Button>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setPage((p) => p + 1)}
                  disabled={!data?.hasNext || loading}
                >
                  Next
                </Button>
              </div>
            </div>
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
        description={`This will permanently remove "${toDelete?.name}". This action cannot be undone.`}
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
