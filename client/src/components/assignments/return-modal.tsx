"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import {
  ReturnCondition,
  type InventoryAssignmentDto,
  type ReturnInventoryAssignmentDto,
} from "@/lib/types";
import { enumOptions, returnConditionLabels } from "@/lib/utils";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
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
  const outstanding = assignment?.outstandingQuantity ?? 0;
  const [quantity, setQuantity] = useState("");
  const [condition, setCondition] = useState<ReturnCondition>(
    ReturnCondition.Good,
  );
  const [notes, setNotes] = useState("");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (open) {
      setQuantity(String(outstanding || 1));
      setCondition(ReturnCondition.Good);
      setNotes("");
    }
  }, [open, outstanding]);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!assignment) return;

    const qty = Number(quantity);
    if (Number.isNaN(qty) || qty < 1 || qty > outstanding) {
      toast.error(`Quantity must be between 1 and ${outstanding}`);
      return;
    }

    setSaving(true);
    try {
      const key =
        typeof crypto !== "undefined" && "randomUUID" in crypto
          ? crypto.randomUUID()
          : undefined;
      const dto: ReturnInventoryAssignmentDto = {
        assignmentId: assignment.id,
        // Only send a quantity for genuine partial returns; full returns omit it.
        returnQuantity: qty < outstanding ? qty : null,
        returnCondition: condition,
        returnNotes: notes.trim() || null,
      };
      await api.assignments.return(dto, key);
      toast.success("Return processed", {
        description:
          qty < outstanding
            ? `${qty} of ${outstanding} returned; ${outstanding - qty} still out.`
            : `${assignment.equipmentName} returned to stock.`,
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
          ? `${assignment.equipmentName} · ${outstanding} outstanding from ${assignment.userName}`
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
      <form id="return-form" onSubmit={onSubmit} className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <Field label="Quantity" required hint={`Max ${outstanding}`}>
            <Input
              type="number"
              min={1}
              max={outstanding}
              value={quantity}
              onChange={(e) => setQuantity(e.target.value)}
              required
            />
          </Field>
          <Field label="Condition">
            <Select
              value={condition}
              onChange={(e) => setCondition(Number(e.target.value))}
            >
              {enumOptions(ReturnCondition, returnConditionLabels).map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </Select>
          </Field>
        </div>
        <Field label="Return notes">
          <Textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder="Remarks, damage details, etc."
          />
        </Field>
      </form>
    </Modal>
  );
}
