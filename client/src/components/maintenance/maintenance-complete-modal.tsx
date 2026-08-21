"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import type { CompleteMaintenanceDto, MaintenanceScheduleDto } from "@/lib/types";
import { dateInputToIso, toDateInputValue } from "@/lib/utils";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";

/** Records completion of a maintenance occurrence; recurring schedules roll forward automatically. */
export function MaintenanceCompleteModal({
  open,
  onClose,
  schedule,
  onSaved,
}: {
  open: boolean;
  onClose: () => void;
  schedule: MaintenanceScheduleDto | null;
  onSaved: () => void;
}) {
  const [performedAt, setPerformedAt] = useState("");
  const [nextDueAt, setNextDueAt] = useState("");
  const [notes, setNotes] = useState("");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (open) {
      setPerformedAt(toDateInputValue(new Date()));
      setNextDueAt("");
      setNotes("");
    }
  }, [open]);

  const recurs = (schedule?.intervalDays ?? 0) > 0;

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!schedule) return;

    const payload: CompleteMaintenanceDto = {
      performedAt: dateInputToIso(performedAt),
      nextDueAt: nextDueAt ? dateInputToIso(nextDueAt) : null,
      notes: notes.trim() || null,
    };

    setSaving(true);
    try {
      await api.maintenance.complete(schedule.id, payload);
      toast.success("Maintenance recorded", { description: schedule.title });
      onSaved();
      onClose();
    } catch (err) {
      const msg =
        err instanceof ApiError
          ? [err.message, ...err.errors].filter(Boolean).join(" · ")
          : "Failed to record maintenance.";
      toast.error("Failed", { description: msg });
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      size="md"
      title="Record maintenance"
      description={
        schedule
          ? `Mark "${schedule.title}" as performed.`
          : "Mark as performed."
      }
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={saving}>
            Cancel
          </Button>
          <Button form="maintenance-complete-form" type="submit" loading={saving}>
            Record
          </Button>
        </>
      }
    >
      <form
        id="maintenance-complete-form"
        onSubmit={onSubmit}
        className="space-y-4"
      >
        <Field label="Performed on" required>
          <Input
            type="date"
            value={performedAt}
            onChange={(e) => setPerformedAt(e.target.value)}
            required
          />
        </Field>
        <Field
          label="Next due"
          hint={recurs ? "Optional — overrides interval" : "Optional"}
        >
          <Input
            type="date"
            value={nextDueAt}
            onChange={(e) => setNextDueAt(e.target.value)}
          />
        </Field>
        {recurs && !nextDueAt && (
          <p className="rounded-lg border border-white/[0.06] bg-white/[0.02] px-3 py-2 text-xs text-slate-400">
            This schedule recurs every {schedule?.intervalDays} days — the next
            due date will roll forward automatically.
          </p>
        )}
        <Field label="Notes">
          <Textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder="Calibrated within tolerance…"
          />
        </Field>
      </form>
    </Modal>
  );
}
