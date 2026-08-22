import type { LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";

export function EmptyState({
  icon: Icon,
  title,
  description,
  action,
  className,
}: {
  icon: LucideIcon;
  title: string;
  description?: string;
  action?: React.ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "flex flex-col items-center justify-center gap-3 px-6 py-16 text-center",
        className,
      )}
    >
      <div className="flex size-14 items-center justify-center rounded-2xl bg-white/[0.04] ring-1 ring-white/10">
        <Icon className="size-6 text-slate-400" />
      </div>
      <div className="space-y-1">
        <p className="font-medium text-white">{title}</p>
        {description && (
          <p className="mx-auto max-w-sm text-sm text-slate-400">{description}</p>
        )}
      </div>
      {action}
    </div>
  );
}
