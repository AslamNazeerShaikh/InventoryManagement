"use client";

import { Toaster } from "sonner";
import { AuthProvider } from "@/lib/auth-context";

export function Providers({ children }: { children: React.ReactNode }) {
  return (
    <AuthProvider>
      {children}
      <Toaster
        theme="dark"
        position="top-right"
        richColors
        closeButton
        toastOptions={{
          classNames: {
            toast:
              "!bg-ink-800/95 !border !border-white/10 !text-white backdrop-blur-xl !rounded-xl",
            description: "!text-slate-400",
          },
        }}
      />
    </AuthProvider>
  );
}
