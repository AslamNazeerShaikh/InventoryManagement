import Link from "next/link";
import { Compass } from "lucide-react";

export default function NotFound() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-5 p-6 text-center">
      <div className="flex size-16 items-center justify-center rounded-2xl bg-brand-500/10 ring-1 ring-brand-400/20">
        <Compass className="size-7 text-brand-300" />
      </div>
      <div>
        <p className="text-5xl font-semibold gradient-text">404</p>
        <p className="mt-2 text-sm text-slate-400">
          This page could not be found.
        </p>
      </div>
      <Link
        href="/dashboard"
        className="rounded-xl bg-gradient-to-br from-brand-300 to-brand-500 px-4 py-2.5 text-sm font-medium text-ink-950 transition hover:brightness-110"
      >
        Back to dashboard
      </Link>
    </div>
  );
}
