"use client";

import { ChevronLeft, ChevronRight } from "lucide-react";
import { cn } from "@/lib/utils";

export function Pagination({
  page,
  pageSize,
  total,
  onPage,
  className,
}: {
  page: number;
  pageSize: number;
  total: number;
  onPage: (page: number) => void;
  className?: string;
}) {
  const pageCount = Math.max(1, Math.ceil(total / pageSize));
  const from = total === 0 ? 0 : (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, total);

  const btn =
    "flex size-8 items-center justify-center rounded-lg border border-white/10 text-slate-300 transition hover:bg-white/[0.06] hover:text-white disabled:pointer-events-none disabled:opacity-40";

  return (
    <div
      className={cn(
        "flex items-center justify-between gap-3 border-t border-white/[0.06] px-4 py-3",
        className,
      )}
    >
      <p className="text-xs text-slate-500">
        Showing{" "}
        <span className="font-medium text-slate-300">
          {from}–{to}
        </span>{" "}
        of <span className="font-medium text-slate-300">{total}</span>
      </p>
      <div className="flex items-center gap-2">
        <button
          className={btn}
          disabled={page <= 1}
          onClick={() => onPage(page - 1)}
          aria-label="Previous page"
        >
          <ChevronLeft className="size-4" />
        </button>
        <span className="text-xs text-slate-400">
          Page {page} of {pageCount}
        </span>
        <button
          className={btn}
          disabled={page >= pageCount}
          onClick={() => onPage(page + 1)}
          aria-label="Next page"
        >
          <ChevronRight className="size-4" />
        </button>
      </div>
    </div>
  );
}
