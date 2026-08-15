"use client";

import { Toaster } from "sonner";
import { AuthProvider } from "@/lib/auth-context";
import { ThemeProvider, useTheme } from "@/lib/theme-context";

/** Toaster that follows the active theme's light/dark mode and tokens. */
function ThemedToaster() {
  const { theme } = useTheme();
  return (
    <Toaster
      theme={theme.mode}
      position="top-right"
      richColors
      closeButton
      toastOptions={{
        classNames: {
          toast:
            "!bg-surface/95 !border !border-line !text-foreground backdrop-blur-xl !rounded-xl",
          description: "!text-muted",
        },
      }}
    />
  );
}

export function Providers({ children }: { children: React.ReactNode }) {
  return (
    <ThemeProvider>
      <AuthProvider>
        {children}
        <ThemedToaster />
      </AuthProvider>
    </ThemeProvider>
  );
}
