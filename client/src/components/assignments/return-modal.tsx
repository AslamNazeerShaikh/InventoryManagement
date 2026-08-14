"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import type { InventoryAssignmentDto } from "@/lib/types";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Textarea } from "@/components/ui/textarea";

export function ReturnModal({
  open,
  onClose,
  assignment,
  onDone,
}: {
  open: boolean;
  onClose: () => void;
  assignment: InventoryAssignmentDto | null;
  onDone: () => void;
}) {
  const [notes, setNotes] = useState("");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (open) setNotes("");
  }, [open]);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!assignment) return;
    setSaving(true);
    try {
      const key =
        typeof crypto !== "undefined" && "randomUUID" in crypto
          ? crypto.randomUUID()
          : undefined;
      await api.assignments.return(
        { assignmentId: assignment.id, returnNotes: notes.trim() || null },
        key,
      );
      toast.success("Return processed", {
        description: `${assignment.equipmentName} returned to stock.`,
      });
      onDone();
      onClose();
    } catch (err) {
      toast.error("Return failed", {
        description: err instanceof ApiError ? err.message : undefined,
      });
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      size="sm"
      title="Process return"
      description={
        assignment
          ? `${assignment.equipmentName} · ×${assignment.assignedQuantity} from ${assignment.userName}`
          : undefined
      }
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={saving}>
            Cancel
          </Button>
          <Button form="return-form" type="submit" loading={saving}>
            Confirm return
          </Button>
        </>
      }
    >
      <form id="return-form" onSubmit={onSubmit}>
        <Field label="Return notes">
          <Textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder="Condition on return, remarks, etc."
          />
        </Field>
      </form>
    </Modal>
  );
}
