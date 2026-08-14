"use client";

import type { LucideIcon } from "lucide-react";
import { AnimatedNumber } from "@/components/ui/animated-number";
import { cn, type Tone } from "@/lib/utils";

const toneChip: Record<Tone, string> = {
  brand: "from-brand-400/25 to-brand-500/5 text-brand-300",
  success: "from-emerald-400/25 to-emerald-500/5 text-emerald-300",
  warning: "from-amber-400/25 to-amber-500/5 text-amber-300",
  danger: "from-rose-400/25 to-rose-500/5 text-rose-300",
  info: "from-sky-400/25 to-sky-500/5 text-sky-300",
  violet: "from-violet-400/25 to-violet-500/5 text-violet-300",
  neutral: "from-slate-400/25 to-slate-500/5 text-slate-300",
};

export function StatCard({
  icon: Icon,
  label,
  value,
  tone = "brand",
  hint,
  format,
}: {
  icon: LucideIcon;
  label: string;
  value: number;
  tone?: Tone;
  hint?: string;
  format?: (n: number) => string;
}) {
  return (
    <div className="panel card-hover group rounded-2xl p-5 hover:-translate-y-0.5 hover:border-white/15">
      <div
        className={cn(
          "flex size-11 items-center justify-center rounded-xl bg-gradient-to-br ring-1 ring-white/10",
          toneChip[tone],
        )}
      >
        <Icon className="size-5.5" />
      </div>
      <p className="mt-4 text-3xl font-semibold tracking-tight text-white">
        <AnimatedNumber value={value} format={format} />
      </p>
      <p className="mt-1 text-sm text-slate-400">{label}</p>
      {hint && <p className="mt-2 text-xs text-slate-500">{hint}</p>}
    </div>
  );
}
