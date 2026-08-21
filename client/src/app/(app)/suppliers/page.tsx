"use client";

import { useEffect, useMemo, useState } from "react";
import { Building2, Mail, Pencil, Phone, Plus, RefreshCw, Search, Trash2, Truck } from "lucide-react";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import { useAsync } from "@/lib/use-async";
import { useAuth } from "@/lib/auth-context";
import type { SupplierDto } from "@/lib/types";
import { cn } from "@/lib/utils";
import { PageHeader } from "@/components/layout/page-header";
import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { ErrorState } from "@/components/ui/error-state";
import { Pagination } from "@/components/ui/pagination";
import { Table, TBody, TD, TH, THead, TR } from "@/components/ui/table";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { FadeIn } from "@/components/ui/reveal";
import { SupplierFormModal } from "@/components/suppliers/supplier-form-modal";

const PAGE_SIZE = 8;

export default function SuppliersPage() {
  const { canManage, isAdmin } = useAuth();
  const { data, loading, error, reload } = useAsync(() => api.suppliers.list(), []);

  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<SupplierDto | null>(null);
  const [toDelete, setToDelete] = useState<SupplierDto | null>(null);
  const [deleting, setDeleting] = useState(false);

  const filtered = useMemo(() => {
    let list = data ?? [];
    const term = search.trim().toLowerCase();
    if (term) {
      list = list.filter((s) =>
        [s.name, s.contactName, s.email, s.phone]
          .filter(Boolean)
          .some((f) => f!.toLowerCase().includes(term)),
      );
    }
    return list;
  }, [data, search]);

  useEffect(() => setPage(1), [search]);

  const paged = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  async function handleDelete() {
    if (!toDelete) return;
    setDeleting(true);
    try {
      await api.suppliers.remove(toDelete.id);
      toast.success("Supplier deleted", { description: toDelete.name });
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
        icon={Truck}
        title="Suppliers"
        description="Manage vendors, contacts and reorder lead times."
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
                Add supplier
              </Button>
            )}
          </>
        }
      />

      <Card>
        <div className="border-b border-white/[0.06] p-4">
          <div className="relative">
            <Search className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-slate-500" />
            <Input
              placeholder="Search by name, contact, email or phone…"
              className="pl-10"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
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
            icon={Building2}
            title={data && data.length > 0 ? "No matching suppliers" : "No suppliers yet"}
            description={
              data && data.length > 0
                ? "Try adjusting your search."
                : "Add your first supplier to track vendors and lead times."
            }
          />
        ) : (
          <FadeIn>
            <Table>
              <THead>
                <TR className="hover:bg-transparent">
                  <TH>Supplier</TH>
                  <TH className="hidden md:table-cell">Contact</TH>
                  <TH className="hidden lg:table-cell">Lead time</TH>
                  <TH>Items</TH>
                  <TH>Status</TH>
                  {canManage && <TH className="text-right">Actions</TH>}
                </TR>
              </THead>
              <TBody>
                {paged.map((s) => (
                  <TR key={s.id}>
                    <TD>
                      <div className="min-w-0">
                        <p className="truncate font-medium text-white">{s.name}</p>
                        {s.website && (
                          <p className="truncate text-xs text-slate-500">
                            {s.website}
                          </p>
                        )}
                      </div>
                    </TD>
                    <TD className="hidden md:table-cell">
                      <div className="space-y-0.5 text-xs text-slate-400">
                        {s.contactName && <p className="text-slate-300">{s.contactName}</p>}
                        {s.email && (
                          <p className="flex items-center gap-1.5">
                            <Mail className="size-3" /> {s.email}
                          </p>
                        )}
                        {s.phone && (
                          <p className="flex items-center gap-1.5">
                            <Phone className="size-3" /> {s.phone}
                          </p>
                        )}
                        {!s.contactName && !s.email && !s.phone && "—"}
                      </div>
                    </TD>
                    <TD className="hidden text-slate-400 lg:table-cell">
                      {s.leadTimeDays != null ? `${s.leadTimeDays} days` : "—"}
                    </TD>
                    <TD>
                      <Badge tone={s.itemCount > 0 ? "info" : "neutral"}>
                        {s.itemCount}
                      </Badge>
                    </TD>
                    <TD>
                      <Badge tone={s.isActive ? "success" : "neutral"} dot>
                        {s.isActive ? "Active" : "Inactive"}
                      </Badge>
                    </TD>
                    {canManage && (
                      <TD>
                        <div className="flex items-center justify-end gap-1">
                          <button
                            onClick={() => {
                              setEditing(s);
                              setFormOpen(true);
                            }}
                            title="Edit"
                            className="flex size-8 items-center justify-center rounded-lg text-slate-400 transition hover:bg-white/[0.06] hover:text-white"
                          >
                            <Pencil className="size-4" />
                          </button>
                          {isAdmin && (
                            <button
                              onClick={() => setToDelete(s)}
                              title="Delete"
                              className="flex size-8 items-center justify-center rounded-lg text-slate-400 transition hover:bg-white/[0.06] hover:text-rose-300"
                            >
                              <Trash2 className="size-4" />
                            </button>
                          )}
                        </div>
                      </TD>
                    )}
                  </TR>
                ))}
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

      <SupplierFormModal
        open={formOpen}
        onClose={() => setFormOpen(false)}
        initial={editing}
        onSaved={reload}
      />
      <ConfirmDialog
        open={Boolean(toDelete)}
        onClose={() => setToDelete(null)}
        onConfirm={handleDelete}
        loading={deleting}
        title="Delete supplier"
        description={`This will remove "${toDelete?.name}". Suppliers with linked items cannot be deleted.`}
        confirmText="Delete"
      />
    </div>
  );
}
