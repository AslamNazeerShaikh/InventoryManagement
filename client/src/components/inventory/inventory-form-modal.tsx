"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import {
  InventoryStatus,
  type CreateInventoryDto,
  type InventoryDto,
  type LocationDto,
  type SupplierDto,
  type UpdateInventoryDto,
} from "@/lib/types";
import {
  dateInputToIso,
  inventoryStatusLabels,
  toDateInputValue,
} from "@/lib/utils";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Select } from "@/components/ui/select";

interface Draft {
  name: string;
  description: string;
  category: string;
  brand: string;
  model: string;
  serialNumber: string;
  barcode: string;
  quantity: string;
  purchasePrice: string;
  supplier: string;
  location: string;
  expiryDate: string;
  manufactureDate: string;
  notes: string;
  status: InventoryStatus;
  reorderLevel: string;
  reorderQuantity: string;
  supplierId: string;
  locationId: string;
}

function emptyDraft(): Draft {
  return {
    name: "",
    description: "",
    category: "",
    brand: "",
    model: "",
    serialNumber: "",
    barcode: "",
    quantity: "1",
    purchasePrice: "",
    supplier: "",
    location: "",
    expiryDate: "",
    manufactureDate: "",
    notes: "",
    status: InventoryStatus.Available,
    reorderLevel: "",
    reorderQuantity: "",
    supplierId: "",
    locationId: "",
  };
}

function fromDto(dto: InventoryDto): Draft {
  return {
    name: dto.name,
    description: dto.description ?? "",
    category: dto.category ?? "",
    brand: dto.brand ?? "",
    model: dto.model ?? "",
    serialNumber: dto.serialNumber ?? "",
    barcode: dto.barcode ?? "",
    quantity: String(dto.quantity),
    purchasePrice: dto.purchasePrice != null ? String(dto.purchasePrice) : "",
    supplier: dto.supplier ?? "",
    location: dto.location ?? "",
    expiryDate: toDateInputValue(dto.expiryDate),
    manufactureDate: toDateInputValue(dto.manufactureDate),
    notes: dto.notes ?? "",
    status: dto.status,
    reorderLevel: dto.reorderLevel != null ? String(dto.reorderLevel) : "",
    reorderQuantity:
      dto.reorderQuantity != null ? String(dto.reorderQuantity) : "",
    supplierId: dto.supplierId != null ? String(dto.supplierId) : "",
    locationId: dto.locationId != null ? String(dto.locationId) : "",
  };
}

export function InventoryFormModal({
  open,
  onClose,
  initial,
  onSaved,
}: {
  open: boolean;
  onClose: () => void;
  initial?: InventoryDto | null;
  onSaved: () => void;
}) {
  const isEdit = Boolean(initial);
  const [draft, setDraft] = useState<Draft>(emptyDraft());
  const [saving, setSaving] = useState(false);
  const [suppliers, setSuppliers] = useState<SupplierDto[]>([]);
  const [locations, setLocations] = useState<LocationDto[]>([]);

  useEffect(() => {
    if (open) setDraft(initial ? fromDto(initial) : emptyDraft());
  }, [open, initial]);

  // Lazy-load active managed suppliers/locations for the optional linking dropdowns.
  useEffect(() => {
    if (!open) return;
    api.suppliers
      .list(true)
      .then(setSuppliers)
      .catch(() => setSuppliers([]));
    api.locations
      .list(true)
      .then(setLocations)
      .catch(() => setLocations([]));
  }, [open]);

  const set =
    <K extends keyof Draft>(key: K) =>
    (
      e: React.ChangeEvent<
        HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
      >,
    ) =>
      setDraft((d) => ({ ...d, [key]: e.target.value }));

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    const quantity = Number(draft.quantity);
    if (!draft.name.trim()) {
      toast.error("Item name is required");
      return;
    }
    if (Number.isNaN(quantity) || quantity < (isEdit ? 0 : 1)) {
      toast.error(`Quantity must be ${isEdit ? "0 or more" : "at least 1"}`);
      return;
    }

    const base: CreateInventoryDto = {
      name: draft.name.trim(),
      description: draft.description.trim() || null,
      category: draft.category.trim() || null,
      brand: draft.brand.trim() || null,
      model: draft.model.trim() || null,
      serialNumber: draft.serialNumber.trim() || null,
      barcode: draft.barcode.trim() || null,
      quantity,
      purchasePrice: draft.purchasePrice === "" ? null : Number(draft.purchasePrice),
      supplier: draft.supplier.trim() || null,
      location: draft.location.trim() || null,
      expiryDate: dateInputToIso(draft.expiryDate),
      manufactureDate: dateInputToIso(draft.manufactureDate),
      notes: draft.notes.trim() || null,
      reorderLevel:
        draft.reorderLevel === "" ? null : Number(draft.reorderLevel),
      reorderQuantity:
        draft.reorderQuantity === "" ? null : Number(draft.reorderQuantity),
      supplierId: draft.supplierId ? Number(draft.supplierId) : null,
      locationId: draft.locationId ? Number(draft.locationId) : null,
    };

    setSaving(true);
    try {
      const key =
        typeof crypto !== "undefined" && "randomUUID" in crypto
          ? crypto.randomUUID()
          : undefined;
      if (isEdit && initial) {
        const payload: UpdateInventoryDto = { ...base, status: draft.status };
        await api.inventory.update(initial.id, payload, key);
        toast.success("Inventory updated", {
          description: `${base.name} was saved.`,
        });
      } else {
        await api.inventory.create(base, key);
        toast.success("Inventory added", {
          description: `${base.name} is now in stock.`,
        });
      }
      onSaved();
      onClose();
    } catch (err) {
      const msg =
        err instanceof ApiError
          ? [err.message, ...err.errors].filter(Boolean).join(" · ")
          : "Failed to save inventory item.";
      toast.error("Save failed", { description: msg });
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      size="xl"
      title={isEdit ? "Edit inventory item" : "Add inventory item"}
      description={
        isEdit
          ? "Update the details for this equipment."
          : "Register a new item into the catalogue."
      }
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={saving}>
            Cancel
          </Button>
          <Button form="inventory-form" type="submit" loading={saving}>
            {isEdit ? "Save changes" : "Add item"}
          </Button>
        </>
      }
    >
      <form id="inventory-form" onSubmit={onSubmit} className="space-y-5">
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Item name" required className="sm:col-span-2">
            <Input
              value={draft.name}
              onChange={set("name")}
              placeholder="e.g. Digital Thermometer"
              required
            />
          </Field>
          <Field label="Category">
            <Input
              value={draft.category}
              onChange={set("category")}
              placeholder="Diagnostic Equipment"
            />
          </Field>
          <Field label="Supplier">
            <Input value={draft.supplier} onChange={set("supplier")} placeholder="MedSupply Inc" />
          </Field>
          <Field label="Brand">
            <Input value={draft.brand} onChange={set("brand")} placeholder="MedTech Corp" />
          </Field>
          <Field label="Model">
            <Input value={draft.model} onChange={set("model")} placeholder="MT-2026" />
          </Field>
          <Field label="Serial number">
            <Input value={draft.serialNumber} onChange={set("serialNumber")} placeholder="MT2026001" />
          </Field>
          <Field label="Barcode">
            <Input value={draft.barcode} onChange={set("barcode")} placeholder="1234567890123" />
          </Field>
          <Field label="Quantity" required>
            <Input
              type="number"
              min={isEdit ? 0 : 1}
              value={draft.quantity}
              onChange={set("quantity")}
              required
            />
          </Field>
          <Field label="Purchase price">
            <Input
              type="number"
              min={0}
              step="0.01"
              value={draft.purchasePrice}
              onChange={set("purchasePrice")}
              placeholder="89.99"
            />
          </Field>
          <Field label="Manufacture date">
            <Input type="date" value={draft.manufactureDate} onChange={set("manufactureDate")} />
          </Field>
          <Field label="Expiry date">
            <Input type="date" value={draft.expiryDate} onChange={set("expiryDate")} />
          </Field>
          <Field label="Location">
            <Input value={draft.location} onChange={set("location")} placeholder="Storage Room A" />
          </Field>
          <Field label="Managed supplier" hint="Optional link">
            <Select value={draft.supplierId} onChange={set("supplierId")}>
              <option value="">None</option>
              {suppliers.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name}
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Managed location" hint="Optional link">
            <Select value={draft.locationId} onChange={set("locationId")}>
              <option value="">None</option>
              {locations.map((l) => (
                <option key={l.id} value={l.id}>
                  {l.name}
                  {l.code ? ` (${l.code})` : ""}
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Reorder level" hint="Par level">
            <Input
              type="number"
              min={0}
              value={draft.reorderLevel}
              onChange={set("reorderLevel")}
              placeholder="5"
            />
          </Field>
          <Field label="Reorder quantity" hint="Suggested">
            <Input
              type="number"
              min={0}
              value={draft.reorderQuantity}
              onChange={set("reorderQuantity")}
              placeholder="50"
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
                {Object.values(InventoryStatus)
                  .filter((v): v is number => typeof v === "number")
                  .map((v) => (
                    <option key={v} value={v}>
                      {inventoryStatusLabels[v as InventoryStatus]}
                    </option>
                  ))}
              </Select>
            </Field>
          )}
          <Field label="Description" className="sm:col-span-2">
            <Textarea
              value={draft.description}
              onChange={set("description")}
              placeholder="Short description of the equipment"
            />
          </Field>
          <Field label="Notes" className="sm:col-span-2">
            <Textarea value={draft.notes} onChange={set("notes")} placeholder="Additional notes" />
          </Field>
        </div>
      </form>
    </Modal>
  );
}
