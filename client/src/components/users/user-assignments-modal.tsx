"use client";

import { useEffect, useState } from "react";
import { ClipboardList } from "lucide-react";
import { api } from "@/lib/api";
import { AssignmentStatus, type InventoryAssignmentDto, type UserDto } from "@/lib/types";
import { formatDate } from "@/lib/utils";
import { Modal } from "@/components/ui/modal";
import { Spinner } from "@/components/ui/spinner";
import { EmptyState } from "@/components/ui/empty-state";
import { Badge } from "@/components/ui/badge";
import { AssignmentStatusBadge } from "@/components/domain/status-badges";

/**
 * Shows every assignment currently or previously held by one recipient
 * ("who has what"), via GET /api/inventoryassignments/user/{userId}.
 */
export function UserAssignmentsModal({
  open,
  onClose,
  user,
}: {
  open: boolean;
  onClose: () => void;
  user: UserDto | null;
}) {
  const [items, setItems] = useState<InventoryAssignmentDto[] | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    let active = true;
    setItems(null);
    if (open && user) {
      setLoading(true);
      api.assignments
        .byUser(user.id)
        .then((r) => active && setItems(r))
        .catch(() => active && setItems([]))
        .finally(() => active && setLoading(false));
    }
    return () => {
      active = false;
    };
  }, [open, user]);

  const activeCount =
    items?.filter((a) => a.status === AssignmentStatus.Active).length ?? 0;

  return (
    <Modal
      open={open}
      onClose={onClose}
      size="lg"
      title={user ? `Assignments · ${user.name}` : "Assignments"}
      description={user?.email}
    >
      {loading ? (
        <div className="flex justify-center py-10">
          <Spinner />
        </div>
      ) : !items || items.length === 0 ? (
        <EmptyState
          icon={ClipboardList}
          title="No assignments"
          description="This user has no equipment assigned."
        />
      ) : (
        <div className="space-y-3">
          <div className="flex items-center gap-2">
            <Badge tone="info">{activeCount} active</Badge>
            <Badge tone="neutral">{items.length} total</Badge>
          </div>
          <ul className="space-y-2">
            {items.map((a) => (
              <li
                key={a.id}
                className="flex items-center justify-between gap-3 rounded-lg border border-white/[0.05] bg-white/[0.02] px-3 py-2.5"
              >
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium text-white">
                    {a.equipmentName}
                  </p>
                  <p className="truncate text-xs text-slate-500">
                    ×{a.assignedQuantity} · assigned {formatDate(a.assignedDate)}
                    {a.expectedReturnDate
                      ? ` · due ${formatDate(a.expectedReturnDate)}`
                      : ""}
                  </p>
                </div>
                <AssignmentStatusBadge status={a.status} />
              </li>
            ))}
          </ul>
        </div>
      )}
    </Modal>
  );
}
