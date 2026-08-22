import { AlertTriangle, RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export function ErrorState({
  message,
  onRetry,
  className,
}: {
  message?: string;
  onRetry?: () => void;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "flex flex-col items-center justify-center gap-3 px-6 py-14 text-center",
        className,
      )}
    >
      <div className="flex size-14 items-center justify-center rounded-2xl bg-rose-500/10 ring-1 ring-rose-500/20">
        <AlertTriangle className="size-6 text-rose-400" />
      </div>
      <div className="space-y-1">
        <p className="font-medium text-white">Something went wrong</p>
        <p className="mx-auto max-w-sm text-sm text-slate-400">
          {message ?? "We couldn't load this data."}
        </p>
      </div>
      {onRetry && (
        <Button
          variant="secondary"
          size="sm"
          onClick={onRetry}
          leftIcon={<RefreshCw className="size-4" />}
        >
          Try again
        </Button>
      )}
    </div>
  );
}
