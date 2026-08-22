"use client";

import { useState } from "react";
import { AnimatePresence, motion } from "motion/react";
import { Menu } from "lucide-react";
import { SidebarContent } from "@/components/layout/sidebar";
import { UserMenu } from "@/components/layout/user-menu";
import { HealthIndicator } from "@/components/layout/health-indicator";
import { BarcodeLookup } from "@/components/layout/barcode-lookup";
import { Notifications } from "@/components/layout/notifications";

export function AppShell({ children }: { children: React.ReactNode }) {
  const [mobileOpen, setMobileOpen] = useState(false);

  return (
    <div className="min-h-full">
      {/* Desktop sidebar */}
      <aside className="fixed inset-y-0 left-0 z-30 hidden w-[268px] border-r border-white/[0.06] lg:block print:hidden">
        <SidebarContent />
      </aside>

      {/* Mobile drawer */}
      <AnimatePresence>
        {mobileOpen && (
          <>
            <motion.div
              className="fixed inset-0 z-40 bg-ink-950/70 backdrop-blur-sm lg:hidden"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              onClick={() => setMobileOpen(false)}
            />
            <motion.aside
              className="fixed inset-y-0 left-0 z-50 w-[280px] border-r border-white/[0.06] lg:hidden"
              initial={{ x: "-100%" }}
              animate={{ x: 0 }}
              exit={{ x: "-100%" }}
              transition={{ type: "spring", damping: 28, stiffness: 280 }}
            >
              <SidebarContent onNavigate={() => setMobileOpen(false)} />
            </motion.aside>
          </>
        )}
      </AnimatePresence>

      {/* Main column */}
      <div className="lg:pl-[268px] print:pl-0">
        <header className="sticky top-0 z-20 flex h-16 items-center justify-between gap-3 border-b border-white/[0.06] bg-ink-950/50 px-4 backdrop-blur-xl sm:px-6 print:hidden">
          <div className="flex items-center gap-3">
            <button
              onClick={() => setMobileOpen(true)}
              className="rounded-lg p-2 text-slate-300 transition hover:bg-white/[0.06] lg:hidden"
              aria-label="Open navigation"
            >
              <Menu className="size-5" />
            </button>
            <HealthIndicator />
          </div>
          <div className="flex items-center gap-1.5">
            <Notifications />
            <BarcodeLookup />
            <UserMenu />
          </div>
        </header>

        <main className="mx-auto w-full max-w-7xl px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
          {children}
        </main>
      </div>
    </div>
  );
}
