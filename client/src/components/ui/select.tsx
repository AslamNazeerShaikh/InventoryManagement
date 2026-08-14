import { ChevronDown } from "lucide-react";
import { cn } from "@/lib/utils";
import { controlClasses } from "@/components/ui/input";

export function Select({
  className,
  children,
  ...props
}: React.SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    <div className="relative">
      <select
        className={cn(
          controlClasses,
          "h-10 cursor-pointer appearance-none bg-ink-800 pr-9 [&>option]:bg-ink-800 [&>option]:text-white",
          className,
        )}
        {...props}
      >
        {children}
      </select>
      <ChevronDown className="pointer-events-none absolute right-3 top-1/2 size-4 -translate-y-1/2 text-slate-400" />
    </div>
  );
}
