"use client";

import { useEffect, useMemo, useState } from "react";
import { ClipboardList, Pencil, Plus, RefreshCw, Search, Trash2, UserPlus, Users } from "lucide-react";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import { useAsync } from "@/lib/use-async";
import { useAuth } from "@/lib/auth-context";
import { UserRole, type UserDto } from "@/lib/types";
import { cn, formatDate, formatRelativeTime, roleLabels } from "@/lib/utils";
import { PageHeader } from "@/components/layout/page-header";
import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Badge } from "@/components/ui/badge";
import { Avatar } from "@/components/ui/avatar";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { ErrorState } from "@/components/ui/error-state";
import { Pagination } from "@/components/ui/pagination";
import { Table, TBody, TD, TH, THead, TR } from "@/components/ui/table";
import { RoleBadge } from "@/components/domain/status-badges";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { UserFormModal } from "@/components/users/user-form-modal";
import { UserAssignmentsModal } from "@/components/users/user-assignments-modal";
import { FadeIn } from "@/components/ui/reveal";

const PAGE_SIZE = 8;

export default function UsersPage() {
  const { user: currentUser } = useAuth();
  const { data, loading, error, reload } = useAsync(() => api.users.list(), []);

  const [search, setSearch] = useState("");
  const [role, setRole] = useState("all");
  const [active, setActive] = useState("all");
  const [page, setPage] = useState(1);

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<UserDto | null>(null);
  const [toDelete, setToDelete] = useState<UserDto | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [viewAssignmentsFor, setViewAssignmentsFor] = useState<UserDto | null>(
    null,
  );

  const filtered = useMemo(() => {
    let list = data ?? [];
    const term = search.trim().toLowerCase();
    if (term) {
      list = list.filter((u) =>
        [u.name, u.email].some((f) => f.toLowerCase().includes(term)),
      );
    }
    if (role !== "all") list = list.filter((u) => u.role === Number(role));
    if (active !== "all")
      list = list.filter((u) => u.isActive === (active === "active"));
    return list;
  }, [data, search, role, active]);

  useEffect(() => setPage(1), [search, role, active]);

  const paged = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  async function handleDelete() {
    if (!toDelete) return;
    setDeleting(true);
    try {
      await api.users.remove(toDelete.id);
      toast.success("User deleted", { description: toDelete.name });
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
        icon={Users}
        title="Users"
        description="Manage team members, roles and access."
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
            <Button
              onClick={() => {
                setEditing(null);
                setFormOpen(true);
              }}
              leftIcon={<Plus className="size-4" />}
            >
              Invite user
            </Button>
          </>
        }
      />

      <Card>
        <div className="flex flex-col gap-3 border-b border-white/[0.06] p-4 sm:flex-row sm:items-center">
          <div className="relative flex-1">
            <Search className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-slate-500" />
            <Input
              placeholder="Search by name or email…"
              className="pl-10"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          <div className="flex gap-3">
            <Select
              className="min-w-[9rem]"
              value={role}
              onChange={(e) => setRole(e.target.value)}
            >
              <option value="all">All roles</option>
              {Object.values(UserRole)
                .filter((v): v is number => typeof v === "number")
                .map((v) => (
                  <option key={v} value={v}>
                    {roleLabels[v as UserRole]}
                  </option>
                ))}
            </Select>
            <Select
              className="min-w-[8rem]"
              value={active}
              onChange={(e) => setActive(e.target.value)}
            >
              <option value="all">All</option>
              <option value="active">Active</option>
              <option value="inactive">Inactive</option>
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
            icon={UserPlus}
            title={data && data.length > 0 ? "No matching users" : "No users yet"}
            description={
              data && data.length > 0
                ? "Try adjusting your search or filters."
                : "Invite your first team member."
            }
          />
        ) : (
          <FadeIn>
            <Table>
              <THead>
                <TR className="hover:bg-transparent">
                  <TH>User</TH>
                  <TH>Role</TH>
                  <TH>Status</TH>
                  <TH className="hidden lg:table-cell">Last login</TH>
                  <TH className="hidden md:table-cell">Joined</TH>
                  <TH className="text-right">Actions</TH>
                </TR>
              </THead>
              <TBody>
                {paged.map((u) => (
                  <TR key={u.id}>
                    <TD>
                      <div className="flex items-center gap-3">
                        <Avatar name={u.name} size="sm" />
                        <div className="min-w-0">
                          <p className="truncate font-medium text-white">
                            {u.name}
                            {currentUser?.id === u.id && (
                              <span className="ml-2 text-xs text-brand-300">
                                You
                              </span>
                            )}
                          </p>
                          <p className="truncate text-xs text-slate-500">
                            {u.email}
                          </p>
                        </div>
                      </div>
                    </TD>
                    <TD>
                      <RoleBadge role={u.role} />
                    </TD>
                    <TD>
                      <Badge tone={u.isActive ? "success" : "neutral"} dot>
                        {u.isActive ? "Active" : "Inactive"}
                      </Badge>
                    </TD>
                    <TD className="hidden text-slate-400 lg:table-cell">
                      {u.lastLoginAt ? formatRelativeTime(u.lastLoginAt) : "Never"}
                    </TD>
                    <TD className="hidden text-slate-400 md:table-cell">
                      {formatDate(u.createdAt)}
                    </TD>
                    <TD>
                      <div className="flex items-center justify-end gap-1">
                        <button
                          onClick={() => setViewAssignmentsFor(u)}
                          title="View assignments"
                          className="flex size-8 items-center justify-center rounded-lg text-slate-400 transition hover:bg-white/[0.06] hover:text-white"
                        >
                          <ClipboardList className="size-4" />
                        </button>
                        <button
                          onClick={() => {
                            setEditing(u);
                            setFormOpen(true);
                          }}
                          title="Edit"
                          className="flex size-8 items-center justify-center rounded-lg text-slate-400 transition hover:bg-white/[0.06] hover:text-white"
                        >
                          <Pencil className="size-4" />
                        </button>
                        <button
                          onClick={() => setToDelete(u)}
                          disabled={currentUser?.id === u.id}
                          title={
                            currentUser?.id === u.id
                              ? "You cannot delete yourself"
                              : "Delete"
                          }
                          className="flex size-8 items-center justify-center rounded-lg text-slate-400 transition hover:bg-white/[0.06] hover:text-rose-300 disabled:pointer-events-none disabled:opacity-30"
                        >
                          <Trash2 className="size-4" />
                        </button>
                      </div>
                    </TD>
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

      <UserFormModal
        open={formOpen}
        onClose={() => setFormOpen(false)}
        initial={editing}
        onSaved={reload}
      />
      <UserAssignmentsModal
        open={Boolean(viewAssignmentsFor)}
        onClose={() => setViewAssignmentsFor(null)}
        user={viewAssignmentsFor}
      />
      <ConfirmDialog
        open={Boolean(toDelete)}
        onClose={() => setToDelete(null)}
        onConfirm={handleDelete}
        loading={deleting}
        title="Delete user"
        description={`This will permanently remove "${toDelete?.name}". This action cannot be undone.`}
        confirmText="Delete"
      />
    </div>
  );
}
