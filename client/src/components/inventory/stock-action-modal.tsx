"use client";

import { useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import type {
  AdjustStockDto,
  DisposeStockDto,
  InventoryDto,
  LocationDto,
  ReceiveStockDto,
  SupplierDto,
  TransferStockDto,
} from "@/lib/types";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";

export type StockAction = "receive" | "adjust" | "dispose" | "transfer";

const META: Record<
  StockAction,
  { title: string; description: string; cta: string }
> = {
  receive: {
    title: "Receive stock",
    description: "Record incoming stock. Total and available quantity increase.",
    cta: "Receive",
  },
  adjust: {
    title: "Adjust stock",
    description: "Correct counts up or down with a reason (stock take, breakage).",
    cta: "Adjust",
  },
  dispose: {
    title: "Dispose stock",
    description: "Permanently remove available stock from circulation.",
    cta: "Dispose",
  },
  transfer: {
    title: "Transfer item",
    description: "Move this item to a different managed location.",
    cta: "Transfer",
  },
};

/** Unified modal for the four ledger-writing stock operations on a single item. */
export function StockActionModal({
  open,
  onClose,
  action,
  item,
  onSaved,
}: {
  open: boolean;
  onClose: () => void;
  action: StockAction;
  item: InventoryDto;
  onSaved: () => void;
}) {
  const meta = META[action];
  const [quantity, setQuantity] = useState("");
  const [delta, setDelta] = useState("");
  const [unitCost, setUnitCost] = useState("");
  const [supplierId, setSupplierId] = useState("");
  const [toLocationId, setToLocationId] = useState("");
  const [reason, setReason] = useState("");
  const [saving, setSaving] = useState(false);
  const [suppliers, setSuppliers] = useState<SupplierDto[]>([]);
  const [locations, setLocations] = useState<LocationDto[]>([]);

  useEffect(() => {
    if (!open) return;
    setQuantity("");
    setDelta("");
    setUnitCost("");
    setSupplierId(item.supplierId != null ? String(item.supplierId) : "");
    setToLocationId("");
    setReason("");
    if (action === "receive") {
      api.suppliers
        .list(true)
        .then(setSuppliers)
        .catch(() => setSuppliers([]));
    }
    if (action === "transfer") {
      api.locations
        .list(true)
        .then(setLocations)
        .catch(() => setLocations([]));
    }
  }, [open, action, item.supplierId]);

  const transferTargets = useMemo(
    () => locations.filter((l) => l.id !== item.locationId),
    [locations, item.locationId],
  );

  function newKey() {
    return typeof crypto !== "undefined" && "randomUUID" in crypto
      ? crypto.randomUUID()
      : undefined;
  }

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    try {
      const key = newKey();
      if (action === "receive") {
        const qty = Number(quantity);
        if (Number.isNaN(qty) || qty < 1) throw new ValidationError("Quantity must be at least 1");
        const dto: ReceiveStockDto = {
          quantity: qty,
          unitCost: unitCost === "" ? null : Number(unitCost),
          supplierId: supplierId ? Number(supplierId) : null,
          reason: reason.trim() || null,
        };
        await api.inventory.receive(item.id, dto, key);
      } else if (action === "adjust") {
        const d = Number(delta);
        if (Number.isNaN(d) || d === 0) throw new ValidationError("Enter a non-zero adjustment");
        if (!reason.trim()) throw new ValidationError("A reason is required");
        const dto: AdjustStockDto = { quantityDelta: d, reason: reason.trim() };
        await api.inventory.adjust(item.id, dto, key);
      } else if (action === "dispose") {
        const qty = Number(quantity);
        if (Number.isNaN(qty) || qty < 1) throw new ValidationError("Quantity must be at least 1");
        if (!reason.trim()) throw new ValidationError("A reason is required");
        const dto: DisposeStockDto = { quantity: qty, reason: reason.trim() };
        await api.inventory.dispose(item.id, dto, key);
      } else {
        if (!toLocationId) throw new ValidationError("Choose a destination location");
        const dto: TransferStockDto = {
          toLocationId: Number(toLocationId),
          reason: reason.trim() || null,
        };
        await api.inventory.transfer(item.id, dto, key);
      }
      toast.success(`${meta.cta} complete`, { description: item.equipmentName });
      onSaved();
      onClose();
    } catch (err) {
      const msg =
        err instanceof ValidationError
          ? err.message
          : err instanceof ApiError
            ? [err.message, ...err.errors].filter(Boolean).join(" · ")
            : "Operation failed.";
      toast.error(`${meta.cta} failed`, { description: msg });
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      size="md"
      title={meta.title}
      description={meta.description}
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={saving}>
            Cancel
          </Button>
          <Button
            form="stock-action-form"
            type="submit"
            loading={saving}
            variant={action === "dispose" ? "danger" : "primary"}
          >
            {meta.cta}
          </Button>
        </>
      }
    >
      <form id="stock-action-form" onSubmit={onSubmit} className="space-y-4">
        <div className="rounded-lg border border-white/[0.06] bg-white/[0.02] px-3 py-2 text-sm text-slate-400">
          On hand: <span className="text-white">{item.availableQuantity}</span> /{" "}
          {item.quantity}
        </div>

        {action === "receive" && (
          <>
            <div className="grid grid-cols-2 gap-4">
              <Field label="Quantity" required>
                <Input
                  type="number"
                  min={1}
                  value={quantity}
                  onChange={(e) => setQuantity(e.target.value)}
                  required
                />
              </Field>
              <Field label="Unit cost">
                <Input
                  type="number"
                  min={0}
                  step="0.01"
                  value={unitCost}
                  onChange={(e) => setUnitCost(e.target.value)}
                  placeholder="0.00"
                />
              </Field>
            </div>
            <Field label="Supplier">
              <Select value={supplierId} onChange={(e) => setSupplierId(e.target.value)}>
                <option value="">None</option>
                {suppliers.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.name}
                  </option>
                ))}
              </Select>
            </Field>
          </>
        )}

        {action === "adjust" && (
          <Field label="Adjustment (+/−)" required hint="e.g. -3 for breakage">
            <Input
              type="number"
              value={delta}
              onChange={(e) => setDelta(e.target.value)}
              placeholder="-3"
              required
            />
          </Field>
        )}

        {action === "dispose" && (
          <Field label="Quantity" required hint={`Max ${item.availableQuantity}`}>
            <Input
              type="number"
              min={1}
              max={item.availableQuantity}
              value={quantity}
              onChange={(e) => setQuantity(e.target.value)}
              required
            />
          </Field>
        )}

        {action === "transfer" && (
          <Field label="Destination" required>
            <Select
              value={toLocationId}
              onChange={(e) => setToLocationId(e.target.value)}
              required
            >
              <option value="">Select a location…</option>
              {transferTargets.map((l) => (
                <option key={l.id} value={l.id}>
                  {l.name}
                  {l.code ? ` (${l.code})` : ""}
                </option>
              ))}
            </Select>
          </Field>
        )}

        <Field
          label="Reason"
          required={action === "adjust" || action === "dispose"}
        >
          <Textarea
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="Purchase order, stock take, damage report…"
          />
        </Field>
      </form>
    </Modal>
  );
}

/** Small local error type so client-side validation messages surface via the same toast path. */
class ValidationError extends Error {}
