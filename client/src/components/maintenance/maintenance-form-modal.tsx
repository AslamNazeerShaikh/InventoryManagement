"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import {
  MaintenanceStatus,
  MaintenanceType,
  type CreateMaintenanceScheduleDto,
  type InventoryDto,
  type MaintenanceScheduleDto,
  type UpdateMaintenanceScheduleDto,
} from "@/lib/types";
import {
  dateInputToIso,
  enumOptions,
  maintenanceStatusLabels,
  maintenanceTypeLabels,
  toDateInputValue,
} from "@/lib/utils";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";

interface Draft {
  inventoryId: string;
  maintenanceType: MaintenanceType;
  title: string;
  description: string;
  intervalDays: string;
  nextDueAt: string;
  status: MaintenanceStatus;
  notes: string;
}

function emptyDraft(presetInventoryId?: number): Draft {
  return {
    inventoryId: presetInventoryId != null ? String(presetInventoryId) : "",
    maintenanceType: MaintenanceType.Inspection,
    title: "",
    description: "",
    intervalDays: "",
    nextDueAt: "",
    status: MaintenanceStatus.Scheduled,
    notes: "",
  };
}

/**
 * Create/edit modal for a maintenance schedule. On create, an item picker is shown (lazy-loaded);
 * on edit, the item is fixed and a status can be set. Optionally pre-bind to an item.
 */
export function MaintenanceFormModal({
  open,
  onClose,
  initial,
  presetInventoryId,
  onSaved,
}: {
  open: boolean;
  onClose: () => void;
  initial?: MaintenanceScheduleDto | null;
  presetInventoryId?: number;
  onSaved: () => void;
}) {
  const isEdit = Boolean(initial);
  const [draft, setDraft] = useState<Draft>(emptyDraft(presetInventoryId));
  const [items, setItems] = useState<InventoryDto[]>([]);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!open) return;
    setDraft(
      initial
        ? {
            inventoryId: String(initial.inventoryId),
            maintenanceType: initial.maintenanceType,
            title: initial.title,
            description: initial.description ?? "",
            intervalDays:
              initial.intervalDays != null ? String(initial.intervalDays) : "",
            nextDueAt: toDateInputValue(initial.nextDueAt),
            status: initial.status,
            notes: initial.notes ?? "",
          }
        : emptyDraft(presetInventoryId),
    );
  }, [open, initial, presetInventoryId]);

  // Lazy-load the item picker only when creating a standalone schedule.
  useEffect(() => {
    if (open && !isEdit && presetInventoryId == null && items.length === 0) {
      api.inventory
        .list()
        .then(setItems)
        .catch(() => setItems([]));
    }
  }, [open, isEdit, presetInventoryId, items.length]);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!isEdit && !draft.inventoryId) {
      toast.error("Please select an item");
      return;
    }
    if (!draft.title.trim()) {
      toast.error("Title is required");
      return;
    }
    const nextDueIso = dateInputToIso(draft.nextDueAt);
    if (!nextDueIso) {
      toast.error("A valid next-due date is required");
      return;
    }
    const interval = draft.intervalDays.trim() ? Number(draft.intervalDays) : null;
    if (interval != null && (Number.isNaN(interval) || interval < 1)) {
      toast.error("Interval must be a positive number of days");
      return;
    }

    setSaving(true);
    try {
      if (isEdit && initial) {
        const payload: UpdateMaintenanceScheduleDto = {
          maintenanceType: draft.maintenanceType,
          title: draft.title.trim(),
          description: draft.description.trim() || null,
          intervalDays: interval,
          nextDueAt: nextDueIso,
          status: draft.status,
          notes: draft.notes.trim() || null,
        };
        await api.maintenance.update(initial.id, payload);
        toast.success("Schedule updated", { description: draft.title });
      } else {
        const payload: CreateMaintenanceScheduleDto = {
          inventoryId: Number(draft.inventoryId),
          maintenanceType: draft.maintenanceType,
          title: draft.title.trim(),
          description: draft.description.trim() || null,
          intervalDays: interval,
          nextDueAt: nextDueIso,
          notes: draft.notes.trim() || null,
        };
        await api.maintenance.create(payload);
        toast.success("Schedule created", { description: draft.title });
      }
      onSaved();
      onClose();
    } catch (err) {
      const msg =
        err instanceof ApiError
          ? [err.message, ...err.errors].filter(Boolean).join(" · ")
          : "Failed to save schedule.";
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
      title={isEdit ? "Edit schedule" : "Add maintenance schedule"}
      description={
        isEdit
          ? "Update this service schedule."
          : "Schedule recurring or one-off service for a durable asset."
      }
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={saving}>
            Cancel
          </Button>
          <Button form="maintenance-form" type="submit" loading={saving}>
            {isEdit ? "Save changes" : "Add schedule"}
          </Button>
        </>
      }
    >
      <form id="maintenance-form" onSubmit={onSubmit} className="space-y-5">
        <div className="grid gap-4 sm:grid-cols-2">
          {!isEdit && presetInventoryId == null && (
            <Field label="Item" required className="sm:col-span-2">
              <Select
                value={draft.inventoryId}
                onChange={(e) =>
                  setDraft((d) => ({ ...d, inventoryId: e.target.value }))
                }
                required
              >
                <option value="">Select an item…</option>
                {items.map((i) => (
                  <option key={i.id} value={i.id}>
                    {i.equipmentName}
                    {i.serialNumber ? ` · ${i.serialNumber}` : ""}
                  </option>
                ))}
              </Select>
            </Field>
          )}
          <Field label="Type">
            <Select
              value={draft.maintenanceType}
              onChange={(e) =>
                setDraft((d) => ({
                  ...d,
                  maintenanceType: Number(e.target.value),
                }))
              }
            >
              {enumOptions(MaintenanceType, maintenanceTypeLabels).map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Next due" required>
            <Input
              type="date"
              value={draft.nextDueAt}
              onChange={(e) =>
                setDraft((d) => ({ ...d, nextDueAt: e.target.value }))
              }
              required
            />
          </Field>
          <Field label="Title" required className="sm:col-span-2">
            <Input
              value={draft.title}
              onChange={(e) => setDraft((d) => ({ ...d, title: e.target.value }))}
              placeholder="Annual calibration"
              required
            />
          </Field>
          <Field
            label="Interval (days)"
            hint="Leave blank for one-off"
          >
            <Input
              type="number"
              min={1}
              value={draft.intervalDays}
              onChange={(e) =>
                setDraft((d) => ({ ...d, intervalDays: e.target.value }))
              }
              placeholder="365"
            />
          </Field>
          {isEdit && (
            <Field label="Status">
              <Select
                value={draft.status}
                onChange={(e) =>
                  setDraft((d) => ({ ...d, status: Number(e.target.value) }))
                }
              >
                {enumOptions(MaintenanceStatus, maintenanceStatusLabels).map(
                  (o) => (
                    <option key={o.value} value={o.value}>
                      {o.label}
                    </option>
                  ),
                )}
              </Select>
            </Field>
          )}
          <Field label="Description" className="sm:col-span-2">
            <Textarea
              value={draft.description}
              onChange={(e) =>
                setDraft((d) => ({ ...d, description: e.target.value }))
              }
              placeholder="Calibrate against reference standard…"
            />
          </Field>
        </div>
      </form>
    </Modal>
  );
}
