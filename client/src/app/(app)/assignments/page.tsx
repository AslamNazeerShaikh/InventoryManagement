"use client";

import { useEffect, useMemo, useState } from "react";
import {
  ClipboardList,
  CornerUpLeft,
  Pencil,
  Plus,
  RefreshCw,
  Search,
} from "lucide-react";
import { api } from "@/lib/api";
import { useAsync } from "@/lib/use-async";
import { useAuth } from "@/lib/auth-context";
import { AssignmentStatus, type InventoryAssignmentDto } from "@/lib/types";
import {
  assignmentStatusLabels,
  cn,
  daysUntil,
  formatDate,
} from "@/lib/utils";
import { PageHeader } from "@/components/layout/page-header";
import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Avatar } from "@/components/ui/avatar";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { ErrorState } from "@/components/ui/error-state";
import { Pagination } from "@/components/ui/pagination";
import { Table, TBody, TD, TH, THead, TR } from "@/components/ui/table";
import { AssignmentStatusBadge } from "@/components/domain/status-badges";
import { AssignmentFormModal } from "@/components/assignments/assignment-form-modal";
import { ReturnModal } from "@/components/assignments/return-modal";
import { FadeIn } from "@/components/ui/reveal";

const PAGE_SIZE = 8;

export default function AssignmentsPage() {
  const { canManage } = useAuth();
  const { data, loading, error, reload } = useAsync(
    () => (canManage ? api.assignments.list() : api.assignments.mine()),
    [canManage],
  );

  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("all");
  const [page, setPage] = useState(1);

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<InventoryAssignmentDto | null>(null);
  const [returning, setReturning] = useState<InventoryAssignmentDto | null>(null);

  const filtered = useMemo(() => {
    let list = data ?? [];
    const term = search.trim().toLowerCase();
    if (term) {
      list = list.filter((a) =>
        [a.equipmentName, a.userName, a.userEmail, a.barcode]
          .filter(Boolean)
          .some((f) => f!.toLowerCase().includes(term)),
      );
    }
    if (status === "overdue") {
      list = list.filter(
        (a) =>
          a.status === AssignmentStatus.Active &&
          (daysUntil(a.expectedReturnDate) ?? 1) < 0,
      );
    } else if (status !== "all") {
      list = list.filter((a) => a.status === Number(status));
    }
    return list;
  }, [data, search, status]);

  useEffect(() => setPage(1), [search, status]);

  const paged = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  return (
    <div className="space-y-6">
      <PageHeader
        icon={ClipboardList}
        title="Assignments"
        description={
          canManage
            ? "Allocate equipment and track returns."
            : "Equipment currently assigned to you."
        }
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
                New assignment
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
              placeholder="Search by item or assignee…"
              className="pl-10"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          <Select
            className="min-w-[10rem]"
            value={status}
            onChange={(e) => setStatus(e.target.value)}
          >
            <option value="all">All assignments</option>
            <option value="overdue">Overdue</option>
            {Object.values(AssignmentStatus)
              .filter((v): v is number => typeof v === "number")
              .map((v) => (
                <option key={v} value={v}>
                  {assignmentStatusLabels[v as AssignmentStatus]}
                </option>
              ))}
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
            icon={ClipboardList}
            title={
              data && data.length > 0
                ? "No matching assignments"
                : "No assignments yet"
            }
            description={
              data && data.length > 0
                ? "Try adjusting your search or filters."
                : canManage
                  ? "Create an assignment to allocate equipment."
                  : "You have no equipment assigned right now."
            }
          />
        ) : (
          <FadeIn>
            <Table>
              <THead>
                <TR className="hover:bg-transparent">
                  <TH>Item</TH>
                  {canManage && <TH>Assignee</TH>}
                  <TH>Qty</TH>
                  <TH className="hidden md:table-cell">Assigned</TH>
                  <TH className="hidden lg:table-cell">Expected return</TH>
                  <TH>Status</TH>
                  {canManage && <TH className="text-right">Actions</TH>}
                </TR>
              </THead>
              <TBody>
                {paged.map((a) => {
                  const overdueDays = daysUntil(a.expectedReturnDate);
                  const isOverdue =
                    a.status === AssignmentStatus.Active &&
                    overdueDays != null &&
                    overdueDays < 0;
                  return (
                    <TR key={a.id}>
                      <TD>
                        <div className="min-w-0">
                          <p className="truncate font-medium text-white">
                            {a.equipmentName}
                          </p>
                          <p className="truncate text-xs text-slate-500">
                            {a.category ?? a.barcode ?? "—"}
                          </p>
                        </div>
                      </TD>
                      {canManage && (
                        <TD>
                          <div className="flex items-center gap-2.5">
                            <Avatar name={a.userName} size="sm" />
                            <div className="min-w-0">
                              <p className="truncate text-sm text-slate-200">
                                {a.userName}
                              </p>
                              <p className="truncate text-xs text-slate-500">
                                {a.userEmail}
                              </p>
                            </div>
                          </div>
                        </TD>
                      )}
                      <TD className="font-medium text-slate-200">
                        ×{a.assignedQuantity}
                      </TD>
                      <TD className="hidden text-slate-400 md:table-cell">
                        {formatDate(a.assignedDate)}
                      </TD>
                      <TD className="hidden lg:table-cell">
                        <span
                          className={cn(
                            isOverdue ? "text-rose-300" : "text-slate-400",
                          )}
                        >
                          {formatDate(a.expectedReturnDate)}
                          {isOverdue && (
                            <span className="ml-1 text-xs">
                              ({Math.abs(overdueDays!)}d late)
                            </span>
                          )}
                        </span>
                      </TD>
                      <TD>
                        <AssignmentStatusBadge status={a.status} />
                      </TD>
                      {canManage && (
                        <TD>
                          <div className="flex items-center justify-end gap-1">
                            {a.status === AssignmentStatus.Active && (
                              <>
                                <Button
                                  variant="subtle"
                                  size="sm"
                                  onClick={() => setReturning(a)}
                                  leftIcon={<CornerUpLeft className="size-3.5" />}
                                >
                                  Return
                                </Button>
                                <button
                                  onClick={() => {
                                    setEditing(a);
                                    setFormOpen(true);
                                  }}
                                  title="Edit"
                                  className="flex size-8 items-center justify-center rounded-lg text-slate-400 transition hover:bg-white/[0.06] hover:text-white"
                                >
                                  <Pencil className="size-4" />
                                </button>
                              </>
                            )}
                          </div>
                        </TD>
                      )}
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

      <AssignmentFormModal
        open={formOpen}
        onClose={() => setFormOpen(false)}
        initial={editing}
        onSaved={reload}
      />
      <ReturnModal
        open={Boolean(returning)}
        onClose={() => setReturning(null)}
        assignment={returning}
        onDone={reload}
      />
    </div>
  );
}
