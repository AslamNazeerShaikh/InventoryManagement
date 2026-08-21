"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import type {
  CreateLocationDto,
  LocationDto,
  UpdateLocationDto,
} from "@/lib/types";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { Switch } from "@/components/ui/switch";

interface Draft {
  name: string;
  code: string;
  description: string;
  parentLocationId: string;
  isActive: boolean;
}

function emptyDraft(): Draft {
  return { name: "", code: "", description: "", parentLocationId: "", isActive: true };
}

/** Create/edit modal for a managed storage location, including an optional parent for hierarchy. */
export function LocationFormModal({
  open,
  onClose,
  initial,
  locations,
  onSaved,
}: {
  open: boolean;
  onClose: () => void;
  initial?: LocationDto | null;
  locations: LocationDto[];
  onSaved: () => void;
}) {
  const isEdit = Boolean(initial);
  const [draft, setDraft] = useState<Draft>(emptyDraft());
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (open) {
      setDraft(
        initial
          ? {
              name: initial.name,
              code: initial.code ?? "",
              description: initial.description ?? "",
              parentLocationId:
                initial.parentLocationId != null
                  ? String(initial.parentLocationId)
                  : "",
              isActive: initial.isActive,
            }
          : emptyDraft(),
      );
    }
  }, [open, initial]);

  // A location cannot be its own parent (deeper cycles are also rejected server-side).
  const parentOptions = locations.filter((l) => !initial || l.id !== initial.id);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!draft.name.trim()) {
      toast.error("Location name is required");
      return;
    }

    const base: CreateLocationDto = {
      name: draft.name.trim(),
      code: draft.code.trim() || null,
      description: draft.description.trim() || null,
      parentLocationId: draft.parentLocationId
        ? Number(draft.parentLocationId)
        : null,
    };

    setSaving(true);
    try {
      if (isEdit && initial) {
        const payload: UpdateLocationDto = { ...base, isActive: draft.isActive };
        await api.locations.update(initial.id, payload);
        toast.success("Location updated", { description: draft.name });
      } else {
        await api.locations.create(base);
        toast.success("Location created", { description: draft.name });
      }
      onSaved();
      onClose();
    } catch (err) {
      const msg =
        err instanceof ApiError
          ? [err.message, ...err.errors].filter(Boolean).join(" · ")
          : "Failed to save location.";
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
      title={isEdit ? "Edit location" : "Add location"}
      description={
        isEdit
          ? "Update this location's details and hierarchy."
          : "Add a site, room or bin. Optionally nest it under a parent."
      }
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={saving}>
            Cancel
          </Button>
          <Button form="location-form" type="submit" loading={saving}>
            {isEdit ? "Save changes" : "Add location"}
          </Button>
        </>
      }
    >
      <form id="location-form" onSubmit={onSubmit} className="space-y-5">
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Name" required>
            <Input
              value={draft.name}
              onChange={(e) => setDraft((d) => ({ ...d, name: e.target.value }))}
              placeholder="Central Store"
              required
            />
          </Field>
          <Field label="Code" hint="Optional, unique">
            <Input
              value={draft.code}
              onChange={(e) => setDraft((d) => ({ ...d, code: e.target.value }))}
              placeholder="CS-01"
            />
          </Field>
          <Field label="Parent location" className="sm:col-span-2">
            <Select
              value={draft.parentLocationId}
              onChange={(e) =>
                setDraft((d) => ({ ...d, parentLocationId: e.target.value }))
              }
            >
              <option value="">None (top level)</option>
              {parentOptions.map((l) => (
                <option key={l.id} value={l.id}>
                  {l.name}
                  {l.code ? ` (${l.code})` : ""}
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Description" className="sm:col-span-2">
            <Textarea
              value={draft.description}
              onChange={(e) =>
                setDraft((d) => ({ ...d, description: e.target.value }))
              }
              placeholder="Main storeroom on the ground floor…"
            />
          </Field>
        </div>

        {isEdit && (
          <Switch
            label="Active location"
            description="Available as a transfer destination"
            checked={draft.isActive}
            onChange={(v) => setDraft((d) => ({ ...d, isActive: v }))}
          />
        )}
      </form>
    </Modal>
  );
}
