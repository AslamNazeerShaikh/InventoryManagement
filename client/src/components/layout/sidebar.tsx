"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { motion } from "motion/react";
import { Activity, LogOut } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { navItems } from "@/components/layout/nav";
import { APP_NAME, APP_TAGLINE } from "@/lib/config";
import { Avatar } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { cn, roleLabels, roleTones } from "@/lib/utils";

function NavLinks({ onNavigate }: { onNavigate?: () => void }) {
  const pathname = usePathname();
  const { canManageUsers, canManage } = useAuth();
  const items = navItems.filter((i) => i.visible({ canManageUsers, canManage }));

  return (
    <nav className="flex flex-1 flex-col gap-1 px-3">
      {items.map((item) => {
        const active =
          pathname === item.href || pathname.startsWith(`${item.href}/`);
        const Icon = item.icon;
        return (
          <Link
            key={item.href}
            href={item.href}
            onClick={onNavigate}
            className={cn(
              "group relative flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition",
              active
                ? "text-white"
                : "text-slate-400 hover:bg-white/[0.04] hover:text-white",
            )}
          >
            {active && (
              <motion.span
                layoutId="nav-active"
                className="absolute inset-0 rounded-xl bg-gradient-to-r from-brand-500/20 to-brand-400/[0.04] ring-1 ring-brand-400/25"
                transition={{ type: "spring", damping: 22, stiffness: 250 }}
              />
            )}
            <Icon
              className={cn(
                "relative size-4.5 transition",
                active ? "text-brand-300" : "text-slate-400 group-hover:text-white",
              )}
            />
            <span className="relative">{item.label}</span>
          </Link>
        );
      })}
    </nav>
  );
}

export function SidebarContent({ onNavigate }: { onNavigate?: () => void }) {
  const { user, logout } = useAuth();

  return (
    <div className="flex h-full flex-col bg-ink-950/80 backdrop-blur-2xl">
      <Link
        href="/dashboard"
        onClick={onNavigate}
        className="flex items-center gap-3 px-6 py-5"
      >
        <div className="flex size-10 items-center justify-center rounded-xl bg-gradient-to-br from-brand-300 to-brand-600 shadow-glow">
          <Activity className="size-5.5 text-ink-950" strokeWidth={2.5} />
        </div>
        <div className="leading-tight">
          <p className="text-base font-semibold gradient-text">{APP_NAME}</p>
          <p className="text-[11px] font-medium uppercase tracking-wider text-slate-500">
            {APP_TAGLINE}
          </p>
        </div>
      </Link>

      <div className="mt-2 flex-1 overflow-y-auto">
        <NavLinks onNavigate={onNavigate} />
      </div>

      {user && (
        <div className="border-t border-white/[0.06] p-3">
          <div className="flex items-center gap-3 rounded-xl px-2 py-2">
            <Avatar name={user.name} size="sm" />
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-medium text-white">
                {user.name}
              </p>
              <div className="mt-0.5">
                <Badge tone={roleTones[user.role]}>
                  {roleLabels[user.role]}
                </Badge>
              </div>
            </div>
            <button
              onClick={() => logout()}
              title="Sign out"
              className="rounded-lg p-2 text-slate-400 transition hover:bg-rose-500/10 hover:text-rose-300"
            >
              <LogOut className="size-4" />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
