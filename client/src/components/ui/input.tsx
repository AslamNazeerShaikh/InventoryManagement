import { cn } from "@/lib/utils";

export const controlClasses =
  "w-full rounded-xl border border-white/10 bg-ink-800/60 px-3.5 text-sm text-white placeholder:text-slate-500 outline-none transition focus:border-brand-400/60 focus:ring-2 focus:ring-brand-400/20 disabled:cursor-not-allowed disabled:opacity-50";

export function Input({
  className,
  ...props
}: React.InputHTMLAttributes<HTMLInputElement>) {
  return <input className={cn(controlClasses, "h-10", className)} {...props} />;
}
