import type { LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";

export function PageHeader({
  title,
  description,
  icon: Icon,
  actions,
  className,
}: {
  title: string;
  description?: string;
  icon?: LucideIcon;
  actions?: React.ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between",
        className,
      )}
    >
      <div className="flex items-center gap-3.5">
        {Icon && (
          <div className="flex size-11 shrink-0 items-center justify-center rounded-2xl bg-brand-500/10 ring-1 ring-brand-400/20">
            <Icon className="size-5.5 text-brand-300" />
          </div>
        )}
        <div className="space-y-0.5">
          <h1 className="text-xl font-semibold tracking-tight text-white sm:text-2xl">
            {title}
          </h1>
          {description && (
            <p className="text-sm text-slate-400">{description}</p>
          )}
        </div>
      </div>
      {actions && (
        <div className="flex items-center gap-2.5">{actions}</div>
      )}
    </div>
  );
}
