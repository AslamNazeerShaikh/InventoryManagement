"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import type {
  CreateSupplierDto,
  SupplierDto,
  UpdateSupplierDto,
} from "@/lib/types";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Switch } from "@/components/ui/switch";

interface Draft {
  name: string;
  contactName: string;
  email: string;
  phone: string;
  address: string;
  website: string;
  leadTimeDays: string;
  notes: string;
  isActive: boolean;
}

function emptyDraft(): Draft {
  return {
    name: "",
    contactName: "",
    email: "",
    phone: "",
    address: "",
    website: "",
    leadTimeDays: "",
    notes: "",
    isActive: true,
  };
}

/** Create/edit modal for a managed supplier. Mirrors the user-form modal conventions. */
export function SupplierFormModal({
  open,
  onClose,
  initial,
  onSaved,
}: {
  open: boolean;
  onClose: () => void;
  initial?: SupplierDto | null;
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
              contactName: initial.contactName ?? "",
              email: initial.email ?? "",
              phone: initial.phone ?? "",
              address: initial.address ?? "",
              website: initial.website ?? "",
              leadTimeDays:
                initial.leadTimeDays != null ? String(initial.leadTimeDays) : "",
              notes: initial.notes ?? "",
              isActive: initial.isActive,
            }
          : emptyDraft(),
      );
    }
  }, [open, initial]);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!draft.name.trim()) {
      toast.error("Supplier name is required");
      return;
    }

    const leadTime = draft.leadTimeDays.trim()
      ? Number(draft.leadTimeDays)
      : null;
    if (leadTime != null && (Number.isNaN(leadTime) || leadTime < 0)) {
      toast.error("Lead time must be a non-negative number of days");
      return;
    }

    const base: CreateSupplierDto = {
      name: draft.name.trim(),
      contactName: draft.contactName.trim() || null,
      email: draft.email.trim() || null,
      phone: draft.phone.trim() || null,
      address: draft.address.trim() || null,
      website: draft.website.trim() || null,
      leadTimeDays: leadTime,
      notes: draft.notes.trim() || null,
    };

    setSaving(true);
    try {
      if (isEdit && initial) {
        const payload: UpdateSupplierDto = { ...base, isActive: draft.isActive };
        await api.suppliers.update(initial.id, payload);
        toast.success("Supplier updated", { description: draft.name });
      } else {
        await api.suppliers.create(base);
        toast.success("Supplier created", { description: draft.name });
      }
      onSaved();
      onClose();
    } catch (err) {
      const msg =
        err instanceof ApiError
          ? [err.message, ...err.errors].filter(Boolean).join(" · ")
          : "Failed to save supplier.";
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
      title={isEdit ? "Edit supplier" : "Add supplier"}
      description={
        isEdit
          ? "Update this supplier's contact details and lead time."
          : "Add a vendor with contact details and a reorder lead time."
      }
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={saving}>
            Cancel
          </Button>
          <Button form="supplier-form" type="submit" loading={saving}>
            {isEdit ? "Save changes" : "Add supplier"}
          </Button>
        </>
      }
    >
      <form id="supplier-form" onSubmit={onSubmit} className="space-y-5">
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Name" required className="sm:col-span-2">
            <Input
              value={draft.name}
              onChange={(e) => setDraft((d) => ({ ...d, name: e.target.value }))}
              placeholder="Acme Medical Supplies"
              required
            />
          </Field>
          <Field label="Contact person">
            <Input
              value={draft.contactName}
              onChange={(e) =>
                setDraft((d) => ({ ...d, contactName: e.target.value }))
              }
              placeholder="Jane Doe"
            />
          </Field>
          <Field label="Email">
            <Input
              type="email"
              value={draft.email}
              onChange={(e) => setDraft((d) => ({ ...d, email: e.target.value }))}
              placeholder="sales@acme.com"
            />
          </Field>
          <Field label="Phone">
            <Input
              value={draft.phone}
              onChange={(e) => setDraft((d) => ({ ...d, phone: e.target.value }))}
              placeholder="+1 555 010 0000"
            />
          </Field>
          <Field label="Lead time (days)" hint="For reorder planning">
            <Input
              type="number"
              min={0}
              value={draft.leadTimeDays}
              onChange={(e) =>
                setDraft((d) => ({ ...d, leadTimeDays: e.target.value }))
              }
              placeholder="7"
            />
          </Field>
          <Field label="Website" className="sm:col-span-2">
            <Input
              value={draft.website}
              onChange={(e) =>
                setDraft((d) => ({ ...d, website: e.target.value }))
              }
              placeholder="https://acme.com"
            />
          </Field>
          <Field label="Address" className="sm:col-span-2">
            <Input
              value={draft.address}
              onChange={(e) =>
                setDraft((d) => ({ ...d, address: e.target.value }))
              }
              placeholder="123 Supply St, City"
            />
          </Field>
          <Field label="Notes" className="sm:col-span-2">
            <Textarea
              value={draft.notes}
              onChange={(e) => setDraft((d) => ({ ...d, notes: e.target.value }))}
              placeholder="Preferred vendor for consumables…"
            />
          </Field>
        </div>

        {isEdit && (
          <Switch
            label="Active supplier"
            description="Available for selection when receiving stock"
            checked={draft.isActive}
            onChange={(v) => setDraft((d) => ({ ...d, isActive: v }))}
          />
        )}
      </form>
    </Modal>
  );
}
