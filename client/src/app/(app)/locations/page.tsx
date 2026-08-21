"use client";

import { useEffect, useMemo, useState } from "react";
import { MapPin, Pencil, Plus, RefreshCw, Search, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import { useAsync } from "@/lib/use-async";
import { useAuth } from "@/lib/auth-context";
import type { LocationDto } from "@/lib/types";
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
import { LocationFormModal } from "@/components/locations/location-form-modal";

const PAGE_SIZE = 8;

export default function LocationsPage() {
  const { canManage, isAdmin } = useAuth();
  const { data, loading, error, reload } = useAsync(() => api.locations.list(), []);

  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<LocationDto | null>(null);
  const [toDelete, setToDelete] = useState<LocationDto | null>(null);
  const [deleting, setDeleting] = useState(false);

  const filtered = useMemo(() => {
    let list = data ?? [];
    const term = search.trim().toLowerCase();
    if (term) {
      list = list.filter((l) =>
        [l.name, l.code, l.parentLocationName]
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
      await api.locations.remove(toDelete.id);
      toast.success("Location deleted", { description: toDelete.name });
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
        icon={MapPin}
        title="Locations"
        description="Manage sites, rooms and bins for stock transfers."
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
                Add location
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
              placeholder="Search by name, code or parent…"
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
            icon={MapPin}
            title={data && data.length > 0 ? "No matching locations" : "No locations yet"}
            description={
              data && data.length > 0
                ? "Try adjusting your search."
                : "Add your first location to enable stock transfers."
            }
          />
        ) : (
          <FadeIn>
            <Table>
              <THead>
                <TR className="hover:bg-transparent">
                  <TH>Location</TH>
                  <TH className="hidden md:table-cell">Code</TH>
                  <TH className="hidden lg:table-cell">Parent</TH>
                  <TH>Items</TH>
                  <TH>Status</TH>
                  {canManage && <TH className="text-right">Actions</TH>}
                </TR>
              </THead>
              <TBody>
                {paged.map((l) => (
                  <TR key={l.id}>
                    <TD>
                      <div className="min-w-0">
                        <p className="truncate font-medium text-white">{l.name}</p>
                        {l.description && (
                          <p className="truncate text-xs text-slate-500">
                            {l.description}
                          </p>
                        )}
                      </div>
                    </TD>
                    <TD className="hidden md:table-cell">
                      {l.code ? (
                        <span className="font-mono text-xs text-slate-300">
                          {l.code}
                        </span>
                      ) : (
                        <span className="text-slate-500">—</span>
                      )}
                    </TD>
                    <TD className="hidden text-slate-400 lg:table-cell">
                      {l.parentLocationName ?? "—"}
                    </TD>
                    <TD>
                      <Badge tone={l.itemCount > 0 ? "info" : "neutral"}>
                        {l.itemCount}
                      </Badge>
                    </TD>
                    <TD>
                      <Badge tone={l.isActive ? "success" : "neutral"} dot>
                        {l.isActive ? "Active" : "Inactive"}
                      </Badge>
                    </TD>
                    {canManage && (
                      <TD>
                        <div className="flex items-center justify-end gap-1">
                          <button
                            onClick={() => {
                              setEditing(l);
                              setFormOpen(true);
                            }}
                            title="Edit"
                            className="flex size-8 items-center justify-center rounded-lg text-slate-400 transition hover:bg-white/[0.06] hover:text-white"
                          >
                            <Pencil className="size-4" />
                          </button>
                          {isAdmin && (
                            <button
                              onClick={() => setToDelete(l)}
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

      <LocationFormModal
        open={formOpen}
        onClose={() => setFormOpen(false)}
        initial={editing}
        locations={data ?? []}
        onSaved={reload}
      />
      <ConfirmDialog
        open={Boolean(toDelete)}
        onClose={() => setToDelete(null)}
        onConfirm={handleDelete}
        loading={deleting}
        title="Delete location"
        description={`This will remove "${toDelete?.name}". Locations with child locations or linked items cannot be deleted.`}
        confirmText="Delete"
      />
    </div>
  );
}
