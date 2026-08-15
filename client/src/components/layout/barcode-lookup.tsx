"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { ScanLine, Search } from "lucide-react";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field } from "@/components/ui/field";

/**
 * Topbar barcode quick-lookup. Works with handheld/USB scanners (which type the
 * code and press Enter into the focused field) and manual entry, then jumps to
 * the item's detail page via GET /api/inventory/barcode/{barcode}.
 */
export function BarcodeLookup() {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [code, setCode] = useState("");
  const [loading, setLoading] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (!open) return;
    setCode("");
    // Auto-focus so a scanner's keystrokes land in the field immediately.
    const t = setTimeout(() => inputRef.current?.focus(), 60);
    return () => clearTimeout(t);
  }, [open]);

  async function lookup(e: React.FormEvent) {
    e.preventDefault();
    const value = code.trim();
    if (!value) return;
    setLoading(true);
    try {
      const item = await api.inventory.byBarcode(value);
      setOpen(false);
      router.push(`/inventory/${item.id}`);
    } catch (err) {
      const description =
        err instanceof ApiError && err.status === 404
          ? `No item is registered with barcode "${value}".`
          : err instanceof ApiError
            ? err.message
            : "Lookup failed. Please try again.";
      toast.error("Barcode not found", { description });
    } finally {
      setLoading(false);
    }
  }

  return (
    <>
      <button
        onClick={() => setOpen(true)}
        title="Scan or look up a barcode"
        aria-label="Scan or look up a barcode"
        className="flex size-9 items-center justify-center rounded-xl text-slate-300 transition hover:bg-white/[0.06] hover:text-white"
      >
        <ScanLine className="size-5" />
      </button>

      <Modal
        open={open}
        onClose={() => setOpen(false)}
        size="sm"
        title="Barcode lookup"
        description="Scan with a handheld scanner or type a barcode, then press Enter."
      >
        <form onSubmit={lookup}>
          <Field label="Barcode">
            <div className="relative">
              <Search className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-slate-500" />
              <Input
                ref={inputRef}
                value={code}
                onChange={(e) => setCode(e.target.value)}
                className="pl-10"
                placeholder="e.g. 1234567890123"
                autoComplete="off"
              />
            </div>
          </Field>
          <div className="mt-5 flex justify-end gap-3">
            <Button
              type="button"
              variant="ghost"
              onClick={() => setOpen(false)}
              disabled={loading}
            >
              Cancel
            </Button>
            <Button type="submit" loading={loading}>
              Find item
            </Button>
          </div>
        </form>
      </Modal>
    </>
  );
}
