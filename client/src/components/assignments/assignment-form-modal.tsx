"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import {
  AssignmentStatus,
  type CreateInventoryAssignmentDto,
  type InventoryAssignmentDto,
  type InventoryDto,
  type UpdateInventoryAssignmentDto,
  type UserDto,
} from "@/lib/types";
import {
  assignmentStatusLabels,
  dateInputToIso,
  toDateInputValue,
} from "@/lib/utils";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { Spinner } from "@/components/ui/spinner";

interface Draft {
  inventoryId: string;
  userId: string;
  assignedQuantity: string;
  expectedReturnDate: string;
  assignmentNotes: string;
  status: AssignmentStatus;
}

export function AssignmentFormModal({
  open,
  onClose,
  initial,
  onSaved,
}: {
  open: boolean;
  onClose: () => void;
  initial?: InventoryAssignmentDto | null;
  onSaved: () => void;
}) {
  const isEdit = Boolean(initial);
  const [draft, setDraft] = useState<Draft>({
    inventoryId: "",
    userId: "",
    assignedQuantity: "1",
    expectedReturnDate: "",
    assignmentNotes: "",
    status: AssignmentStatus.Active,
  });
  const [inventories, setInventories] = useState<InventoryDto[]>([]);
  const [users, setUsers] = useState<UserDto[]>([]);
  const [loadingOpts, setLoadingOpts] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!open) return;
    if (initial) {
      setDraft({
        inventoryId: String(initial.inventoryId),
        userId: String(initial.userId),
        assignedQuantity: String(initial.assignedQuantity),
        expectedReturnDate: toDateInputValue(initial.expectedReturnDate),
        assignmentNotes: initial.assignmentNotes ?? "",
        status: initial.status,
      });
    } else {
      setDraft({
        inventoryId: "",
        userId: "",
        assignedQuantity: "1",
        expectedReturnDate: "",
        assignmentNotes: "",
        status: AssignmentStatus.Active,
      });
      setLoadingOpts(true);
      Promise.all([api.inventory.available(), api.users.active()])
        .then(([inv, us]) => {
          setInventories(inv);
          setUsers(us);
        })
        .catch(() => toast.error("Failed to load options"))
        .finally(() => setLoadingOpts(false));
    }
  }, [open, initial]);

  const selectedInventory = inventories.find(
    (i) => i.id === Number(draft.inventoryId),
  );
  const maxQty = selectedInventory?.availableQuantity;

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    const qty = Number(draft.assignedQuantity);
    if (!isEdit && (!draft.inventoryId || !draft.userId)) {
      toast.error("Select an item and an assignee");
      return;
    }
    if (Number.isNaN(qty) || qty < 1) {
      toast.error("Quantity must be at least 1");
      return;
    }
    if (!isEdit && maxQty != null && qty > maxQty) {
      toast.error(`Only ${maxQty} available for this item`);
      return;
    }

    setSaving(true);
    try {
      const key =
        typeof crypto !== "undefined" && "randomUUID" in crypto
          ? crypto.randomUUID()
          : undefined;
      if (isEdit && initial) {
        const payload: UpdateInventoryAssignmentDto = {
          assignedQuantity: qty,
          expectedReturnDate: dateInputToIso(draft.expectedReturnDate),
          status: draft.status,
          assignmentNotes: draft.assignmentNotes.trim() || null,
        };
        await api.assignments.update(initial.id, payload);
        toast.success("Assignment updated");
      } else {
        const payload: CreateInventoryAssignmentDto = {
          inventoryId: Number(draft.inventoryId),
          userId: Number(draft.userId),
          assignedQuantity: qty,
          expectedReturnDate: dateInputToIso(draft.expectedReturnDate),
          assignmentNotes: draft.assignmentNotes.trim() || null,
        };
        await api.assignments.create(payload, key);
        toast.success("Assignment created");
      }
      onSaved();
      onClose();
    } catch (err) {
      const msg =
        err instanceof ApiError
          ? [err.message, ...err.errors].filter(Boolean).join(" · ")
          : "Failed to save assignment.";
      toast.error("Save failed", { description: msg });
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      size="lg"
      title={isEdit ? "Edit assignment" : "New assignment"}
      description={
        isEdit
          ? "Update the details of this allocation."
          : "Allocate equipment to a team member."
      }
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={saving}>
            Cancel
          </Button>
          <Button form="assignment-form" type="submit" loading={saving}>
            {isEdit ? "Save changes" : "Assign"}
          </Button>
        </>
      }
    >
      {loadingOpts ? (
        <div className="flex justify-center py-10">
          <Spinner className="size-6" />
        </div>
      ) : (
        <form id="assignment-form" onSubmit={onSubmit} className="space-y-4">
          {isEdit ? (
            <div className="rounded-xl border border-white/[0.06] bg-white/[0.02] p-3">
              <p className="text-sm font-medium text-white">
                {initial?.equipmentName}
              </p>
              <p className="text-xs text-slate-500">
                Assigned to {initial?.userName}
              </p>
            </div>
          ) : (
            <div className="grid gap-4 sm:grid-cols-2">
              <Field label="Equipment" required>
                <Select
                  value={draft.inventoryId}
                  onChange={(e) =>
                    setDraft((d) => ({ ...d, inventoryId: e.target.value }))
                  }
                  required
                >
                  <option value="">Select item…</option>
                  {inventories.map((i) => (
                    <option key={i.id} value={i.id}>
                      {i.equipmentName} ({i.availableQuantity} available)
                    </option>
                  ))}
                </Select>
              </Field>
              <Field label="Assignee" required>
                <Select
                  value={draft.userId}
                  onChange={(e) =>
                    setDraft((d) => ({ ...d, userId: e.target.value }))
                  }
                  required
                >
                  <option value="">Select user…</option>
                  {users.map((u) => (
                    <option key={u.id} value={u.id}>
                      {u.name} · {u.email}
                    </option>
                  ))}
                </Select>
              </Field>
            </div>
          )}

          <div className="grid gap-4 sm:grid-cols-2">
            <Field
              label="Quantity"
              required
              hint={!isEdit && maxQty != null ? `Max ${maxQty}` : undefined}
            >
              <Input
                type="number"
                min={1}
                max={!isEdit ? maxQty : undefined}
                value={draft.assignedQuantity}
                onChange={(e) =>
                  setDraft((d) => ({ ...d, assignedQuantity: e.target.value }))
                }
                required
              />
            </Field>
            <Field label="Expected return date">
              <Input
                type="date"
                value={draft.expectedReturnDate}
                onChange={(e) =>
                  setDraft((d) => ({ ...d, expectedReturnDate: e.target.value }))
                }
              />
            </Field>
          </div>

          {isEdit && (
            <Field label="Status">
              <Select
                value={draft.status}
                onChange={(e) =>
                  setDraft((d) => ({ ...d, status: Number(e.target.value) }))
                }
              >
                {Object.values(AssignmentStatus)
                  .filter((v): v is number => typeof v === "number")
                  .map((v) => (
                    <option key={v} value={v}>
                      {assignmentStatusLabels[v as AssignmentStatus]}
                    </option>
                  ))}
              </Select>
            </Field>
          )}

          <Field label="Notes">
            <Textarea
              value={draft.assignmentNotes}
              onChange={(e) =>
                setDraft((d) => ({ ...d, assignmentNotes: e.target.value }))
              }
              placeholder="Assignment context or instructions"
            />
          </Field>
        </form>
      )}
    </Modal>
  );
}
