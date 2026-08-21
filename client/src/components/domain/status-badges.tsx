import { Badge } from "@/components/ui/badge";
import {
  AssignmentStatus,
  InventoryStatus,
  MaintenanceStatus,
  ReturnCondition,
  StockMovementType,
  UserRole,
} from "@/lib/types";
import {
  assignmentStatusLabels,
  assignmentStatusTones,
  inventoryStatusLabels,
  inventoryStatusTones,
  maintenanceStatusLabels,
  maintenanceStatusTones,
  returnConditionLabels,
  returnConditionTones,
  roleLabels,
  roleTones,
  stockMovementLabels,
  stockMovementTones,
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

export function StockMovementBadge({ type }: { type: StockMovementType }) {
  return <Badge tone={stockMovementTones[type]}>{stockMovementLabels[type]}</Badge>;
}

export function MaintenanceStatusBadge({ status }: { status: MaintenanceStatus }) {
  return (
    <Badge tone={maintenanceStatusTones[status]} dot>
      {maintenanceStatusLabels[status]}
    </Badge>
  );
}

export function ReturnConditionBadge({
  condition,
}: {
  condition: ReturnCondition;
}) {
  return (
    <Badge tone={returnConditionTones[condition]}>
      {returnConditionLabels[condition]}
    </Badge>
  );
}
