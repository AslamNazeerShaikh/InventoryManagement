"use client";

import { useEffect, useState } from "react";
import { Boxes, History } from "lucide-react";
import { api } from "@/lib/api";
import type { AssignmentHistoryDto, InventoryDto } from "@/lib/types";
import { formatCurrency, formatDate, formatDateTime } from "@/lib/utils";
import { Modal } from "@/components/ui/modal";
import { Spinner } from "@/components/ui/spinner";
import {
  AssignmentStatusBadge,
  InventoryStatusBadge,
} from "@/components/domain/status-badges";

function Detail({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="min-w-0">
      <dt className="text-xs uppercase tracking-wide text-slate-500">{label}</dt>
      <dd className="mt-0.5 truncate text-sm text-slate-200">{value ?? "—"}</dd>
    </div>
  );
}

export function InventoryDetailsModal({
  open,
  onClose,
  item,
  canViewHistory,
}: {
  open: boolean;
  onClose: () => void;
  item: InventoryDto | null;
  canViewHistory: boolean;
}) {
  const [history, setHistory] = useState<AssignmentHistoryDto | null>(null);
  const [loadingHistory, setLoadingHistory] = useState(false);

  useEffect(() => {
    let active = true;
    setHistory(null);
    if (open && item && canViewHistory) {
      setLoadingHistory(true);
      api.assignments
        .history(item.id)
        .then((h) => active && setHistory(h))
        .catch(() => active && setHistory(null))
        .finally(() => active && setLoadingHistory(false));
    }
    return () => {
      active = false;
    };
  }, [open, item, canViewHistory]);

  if (!item) return null;

  const pct =
    item.quantity > 0
      ? Math.round((item.availableQuantity / item.quantity) * 100)
      : 0;

  return (
    <Modal open={open} onClose={onClose} size="lg" title={item.equipmentName}>
      <div className="space-y-6">
        <div className="flex flex-wrap items-center gap-3">
          <InventoryStatusBadge status={item.status} />
          {item.category && (
            <span className="text-sm text-slate-400">{item.category}</span>
          )}
        </div>

        {/* Stock */}
        <div className="rounded-xl border border-white/[0.06] bg-white/[0.02] p-4">
          <div className="mb-2 flex items-center justify-between text-sm">
            <span className="text-slate-400">Available stock</span>
            <span className="font-medium text-white">
              {item.availableQuantity} / {item.quantity}
            </span>
          </div>
          <div className="h-2 overflow-hidden rounded-full bg-white/[0.06]">
            <div
              className="h-full rounded-full bg-gradient-to-r from-brand-400 to-brand-500"
              style={{ width: `${pct}%` }}
            />
          </div>
        </div>

        <dl className="grid grid-cols-2 gap-4 sm:grid-cols-3">
          <Detail label="Brand" value={item.brand} />
          <Detail label="Model" value={item.model} />
          <Detail label="Serial" value={item.serialNumber} />
          <Detail label="Barcode" value={item.barcode} />
          <Detail label="Supplier" value={item.supplier} />
          <Detail label="Location" value={item.location} />
          <Detail label="Purchase price" value={formatCurrency(item.purchasePrice)} />
          <Detail label="Expiry" value={formatDate(item.expiryDate)} />
          <Detail label="Manufactured" value={formatDate(item.manufactureDate)} />
          <Detail label="Created" value={formatDate(item.createdAt)} />
          <Detail label="Created by" value={item.createdByUserName} />
        </dl>

        {(item.description || item.notes) && (
          <div className="space-y-3 border-t border-white/[0.06] pt-4">
            {item.description && (
              <div>
                <p className="text-xs uppercase tracking-wide text-slate-500">
                  Description
                </p>
                <p className="mt-1 text-sm text-slate-300">{item.description}</p>
              </div>
            )}
            {item.notes && (
              <div>
                <p className="text-xs uppercase tracking-wide text-slate-500">
                  Notes
                </p>
                <p className="mt-1 text-sm text-slate-300">{item.notes}</p>
              </div>
            )}
          </div>
        )}

        {canViewHistory && (
          <div className="border-t border-white/[0.06] pt-4">
            <div className="mb-3 flex items-center gap-2">
              <History className="size-4 text-slate-400" />
              <p className="text-sm font-medium text-white">Assignment history</p>
            </div>
            {loadingHistory ? (
              <div className="flex justify-center py-6">
                <Spinner />
              </div>
            ) : !history || history.assignments.length === 0 ? (
              <div className="flex items-center gap-2 py-4 text-sm text-slate-500">
                <Boxes className="size-4" /> No assignment history yet.
              </div>
            ) : (
              <ul className="space-y-2">
                {history.assignments.map((a) => (
                  <li
                    key={a.id}
                    className="flex items-center justify-between gap-3 rounded-lg border border-white/[0.05] bg-white/[0.02] px-3 py-2"
                  >
                    <div className="min-w-0">
                      <p className="truncate text-sm text-slate-200">
                        {a.userName} · ×{a.assignedQuantity}
                      </p>
                      <p className="truncate text-xs text-slate-500">
                        {formatDateTime(a.assignedDate)}
                        {a.returnDate
                          ? ` · returned ${formatDate(a.returnDate)}`
                          : ""}
                      </p>
                    </div>
                    <AssignmentStatusBadge status={a.status} />
                  </li>
                ))}
              </ul>
            )}
          </div>
        )}
      </div>
    </Modal>
  );
}
