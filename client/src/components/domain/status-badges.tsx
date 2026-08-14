import { Badge } from "@/components/ui/badge";
import { AssignmentStatus, InventoryStatus, UserRole } from "@/lib/types";
import {
  assignmentStatusLabels,
  assignmentStatusTones,
  inventoryStatusLabels,
  inventoryStatusTones,
  roleLabels,
  roleTones,
} from "@/lib/utils";

export function InventoryStatusBadge({ status }: { status: InventoryStatus }) {
  return (
    <Badge tone={inventoryStatusTones[status]} dot>
      {inventoryStatusLabels[status]}
    </Badge>
  );
}

export function AssignmentStatusBadge({ status }: { status: AssignmentStatus }) {
  return (
    <Badge tone={assignmentStatusTones[status]} dot>
      {assignmentStatusLabels[status]}
    </Badge>
  );
}

export function RoleBadge({ role }: { role: UserRole }) {
  return <Badge tone={roleTones[role]}>{roleLabels[role]}</Badge>;
}
