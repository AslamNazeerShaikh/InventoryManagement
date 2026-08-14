"use client";

import { useEffect } from "react";
import { AlertTriangle, RotateCcw } from "lucide-react";

export default function Error({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <div className="flex min-h-screen items-center justify-center p-6">
      <div className="panel w-full max-w-md rounded-2xl p-8 text-center">
        <div className="mx-auto mb-4 flex size-14 items-center justify-center rounded-2xl bg-rose-500/10 ring-1 ring-rose-500/20">
          <AlertTriangle className="size-6 text-rose-400" />
        </div>
        <h1 className="text-lg font-semibold text-white">Something went wrong</h1>
        <p className="mt-1.5 text-sm text-slate-400">
          An unexpected error occurred while rendering this page. You can try
          again, and if it persists, reload the app.
        </p>
        <button
          onClick={reset}
          className="mt-6 inline-flex items-center gap-2 rounded-xl bg-gradient-to-br from-brand-300 to-brand-500 px-4 py-2.5 text-sm font-medium text-ink-950 transition hover:brightness-110"
        >
          <RotateCcw className="size-4" /> Try again
        </button>
      </div>
    </div>
  );
}
