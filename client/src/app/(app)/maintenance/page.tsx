"use client";

import { useEffect, useMemo, useState } from "react";
import {
  CalendarClock,
  CheckCircle2,
  Pencil,
  Plus,
  RefreshCw,
  Search,
  Trash2,
  Wrench,
} from "lucide-react";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import { useAsync } from "@/lib/use-async";
import { useAuth } from "@/lib/auth-context";
import { MaintenanceStatus, type MaintenanceScheduleDto } from "@/lib/types";
import { cn, formatDate, maintenanceTypeLabels } from "@/lib/utils";
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
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { FadeIn } from "@/components/ui/reveal";
import { MaintenanceStatusBadge } from "@/components/domain/status-badges";
import { MaintenanceFormModal } from "@/components/maintenance/maintenance-form-modal";
import { MaintenanceCompleteModal } from "@/components/maintenance/maintenance-complete-modal";

const PAGE_SIZE = 8;

export default function MaintenancePage() {
  const { canManage, isAdmin } = useAuth();
  const { data, loading, error, reload } = useAsync(
    () => api.maintenance.paged(1, 200).then((p) => p.data),
    [],
  );

  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("open");
  const [page, setPage] = useState(1);
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<MaintenanceScheduleDto | null>(null);
  const [completing, setCompleting] = useState<MaintenanceScheduleDto | null>(null);
  const [toDelete, setToDelete] = useState<MaintenanceScheduleDto | null>(null);
  const [deleting, setDeleting] = useState(false);

  const filtered = useMemo(() => {
    let list = data ?? [];
    const term = search.trim().toLowerCase();
    if (term) {
      list = list.filter((m) =>
        [m.title, m.itemName].some((f) => f.toLowerCase().includes(term)),
      );
    }
    if (status === "open") {
      list = list.filter(
        (m) =>
          m.status === MaintenanceStatus.Due ||
          m.status === MaintenanceStatus.Overdue ||
          m.status === MaintenanceStatus.Scheduled,
      );
    } else if (status !== "all") {
      list = list.filter((m) => m.status === Number(status));
    }
    return list;
  }, [data, search, status]);

  useEffect(() => setPage(1), [search, status]);

  const paged = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  async function handleDelete() {
    if (!toDelete) return;
    setDeleting(true);
    try {
      await api.maintenance.remove(toDelete.id);
      toast.success("Schedule deleted", { description: toDelete.title });
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
        icon={Wrench}
        title="Maintenance"
        description="Track service and calibration schedules for durable assets."
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
                Add schedule
              </Button>
            )}
          </>
        }
      />

      <Card>
        <div className="flex flex-col gap-3 border-b border-white/[0.06] p-4 sm:flex-row sm:items-center">
          <div className="relative flex-1">
            <Search className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-slate-500" />
            <Input
              placeholder="Search by title or item…"
              className="pl-10"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          <Select
            className="min-w-[11rem]"
            value={status}
            onChange={(e) => setStatus(e.target.value)}
          >
            <option value="open">Open (due & scheduled)</option>
            <option value="all">All statuses</option>
            <option value={MaintenanceStatus.Overdue}>Overdue</option>
            <option value={MaintenanceStatus.Due}>Due</option>
            <option value={MaintenanceStatus.Scheduled}>Scheduled</option>
            <option value={MaintenanceStatus.Completed}>Completed</option>
            <option value={MaintenanceStatus.Cancelled}>Cancelled</option>
          </Select>
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
            icon={CalendarClock}
            title={data && data.length > 0 ? "Nothing here" : "No schedules yet"}
            description={
              data && data.length > 0
                ? "Try adjusting your search or filter."
                : "Schedule your first service or calibration."
            }
          />
        ) : (
          <FadeIn>
            <Table>
              <THead>
                <TR className="hover:bg-transparent">
                  <TH>Schedule</TH>
                  <TH className="hidden md:table-cell">Item</TH>
                  <TH className="hidden lg:table-cell">Type</TH>
                  <TH>Next due</TH>
                  <TH>Status</TH>
                  {canManage && <TH className="text-right">Actions</TH>}
                </TR>
              </THead>
              <TBody>
                {paged.map((m) => (
                  <TR key={m.id}>
                    <TD>
                      <div className="min-w-0">
                        <p className="truncate font-medium text-white">{m.title}</p>
                        {m.intervalDays != null && (
                          <p className="text-xs text-slate-500">
                            Every {m.intervalDays} days
                          </p>
                        )}
                      </div>
                    </TD>
                    <TD className="hidden truncate text-slate-300 md:table-cell">
                      {m.itemName}
                    </TD>
                    <TD className="hidden text-slate-400 lg:table-cell">
                      {maintenanceTypeLabels[m.maintenanceType]}
                    </TD>
                    <TD className="text-slate-300">{formatDate(m.nextDueAt)}</TD>
                    <TD>
                      <MaintenanceStatusBadge status={m.status} />
                    </TD>
                    {canManage && (
                      <TD>
                        <div className="flex items-center justify-end gap-1">
                          {m.status !== MaintenanceStatus.Completed &&
                            m.status !== MaintenanceStatus.Cancelled && (
                              <button
                                onClick={() => setCompleting(m)}
                                title="Record maintenance"
                                className="flex size-8 items-center justify-center rounded-lg text-slate-400 transition hover:bg-white/[0.06] hover:text-emerald-300"
                              >
                                <CheckCircle2 className="size-4" />
                              </button>
                            )}
                          <button
                            onClick={() => {
                              setEditing(m);
                              setFormOpen(true);
                            }}
                            title="Edit"
                            className="flex size-8 items-center justify-center rounded-lg text-slate-400 transition hover:bg-white/[0.06] hover:text-white"
                          >
                            <Pencil className="size-4" />
                          </button>
                          {isAdmin && (
                            <button
                              onClick={() => setToDelete(m)}
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

      <MaintenanceFormModal
        open={formOpen}
        onClose={() => setFormOpen(false)}
        initial={editing}
        onSaved={reload}
      />
      <MaintenanceCompleteModal
        open={Boolean(completing)}
        onClose={() => setCompleting(null)}
        schedule={completing}
        onSaved={reload}
      />
      <ConfirmDialog
        open={Boolean(toDelete)}
        onClose={() => setToDelete(null)}
        onConfirm={handleDelete}
        loading={deleting}
        title="Delete schedule"
        description={`This will remove "${toDelete?.title}". This action cannot be undone.`}
        confirmText="Delete"
      />
    </div>
  );
}
