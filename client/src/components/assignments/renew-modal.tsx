"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import type {
  InventoryAssignmentDto,
  RenewInventoryAssignmentDto,
} from "@/lib/types";
import { dateInputToIso, formatDate, toDateInputValue } from "@/lib/utils";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";

/** Extends an active assignment's expected return date. */
export function RenewModal({
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
  const [date, setDate] = useState("");
  const [notes, setNotes] = useState("");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (open) {
      // Default to two weeks from today as a convenient starting point.
      const d = new Date();
      d.setDate(d.getDate() + 14);
      setDate(toDateInputValue(d));
      setNotes("");
    }
  }, [open]);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!assignment) return;

    const iso = dateInputToIso(date);
    if (!iso || new Date(iso).getTime() <= Date.now()) {
      toast.error("Choose a future return date");
      return;
    }

    setSaving(true);
    try {
      const dto: RenewInventoryAssignmentDto = {
        assignmentId: assignment.id,
        newExpectedReturnDate: iso,
        notes: notes.trim() || null,
      };
      await api.assignments.renew(dto);
      toast.success("Assignment renewed", {
        description: `${assignment.equipmentName} extended to ${formatDate(iso)}.`,
      });
      onDone();
      onClose();
    } catch (err) {
      toast.error("Renew failed", {
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
      title="Renew assignment"
      description={
        assignment
          ? `${assignment.equipmentName} · ${assignment.userName}${
              assignment.renewalCount > 0
                ? ` · renewed ${assignment.renewalCount}×`
                : ""
            }`
          : undefined
      }
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={saving}>
            Cancel
          </Button>
          <Button form="renew-form" type="submit" loading={saving}>
            Renew
          </Button>
        </>
      }
    >
      <form id="renew-form" onSubmit={onSubmit} className="space-y-4">
        <Field label="New expected return" required>
          <Input
            type="date"
            value={date}
            onChange={(e) => setDate(e.target.value)}
            required
          />
        </Field>
        <Field label="Notes">
          <Textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder="Reason for extension…"
          />
        </Field>
      </form>
    </Modal>
  );
}
