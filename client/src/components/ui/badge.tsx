import { cn, type Tone } from "@/lib/utils";

const toneClasses: Record<Tone, string> = {
  brand: "bg-brand-500/15 text-brand-200 ring-brand-400/25",
  success: "bg-emerald-500/15 text-emerald-300 ring-emerald-400/25",
  warning: "bg-amber-500/15 text-amber-300 ring-amber-400/25",
  danger: "bg-rose-500/15 text-rose-300 ring-rose-400/25",
  info: "bg-sky-500/15 text-sky-300 ring-sky-400/25",
  violet: "bg-violet-500/15 text-violet-300 ring-violet-400/25",
  neutral: "bg-slate-500/15 text-slate-300 ring-slate-400/25",
};

const dotClasses: Record<Tone, string> = {
  brand: "bg-brand-400",
  success: "bg-emerald-400",
  warning: "bg-amber-400",
  danger: "bg-rose-400",
  info: "bg-sky-400",
  violet: "bg-violet-400",
  neutral: "bg-slate-400",
};

export function Badge({
  tone = "neutral",
  dot = false,
  className,
  children,
}: {
  tone?: Tone;
  dot?: boolean;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset",
        toneClasses[tone],
        className,
      )}
    >
      {dot && (
        <span className={cn("size-1.5 rounded-full", dotClasses[tone])} />
      )}
      {children}
    </span>
  );
}
