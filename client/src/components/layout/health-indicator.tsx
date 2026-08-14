"use client";

import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import { cn } from "@/lib/utils";

export function HealthIndicator() {
  const [online, setOnline] = useState<boolean | null>(null);

  useEffect(() => {
    let active = true;
    const check = async () => {
      const ok = await api.health();
      if (active) setOnline(ok);
    };
    check();
    const id = setInterval(check, 30000);
    return () => {
      active = false;
      clearInterval(id);
    };
  }, []);

  const label =
    online == null ? "Checking API" : online ? "API online" : "API offline";

  return (
    <div className="hidden items-center gap-2 rounded-full border border-white/[0.06] bg-white/[0.03] px-3 py-1.5 sm:flex">
      <span className="relative flex size-2">
        {online && (
          <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-emerald-400/70" />
        )}
        <span
          className={cn(
            "relative inline-flex size-2 rounded-full",
            online == null
              ? "bg-slate-400"
              : online
                ? "bg-emerald-400"
                : "bg-rose-400",
          )}
        />
      </span>
      <span className="text-xs font-medium text-slate-300">{label}</span>
    </div>
  );
}
