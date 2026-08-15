"use client";

import { Palette } from "lucide-react";
import { PageHeader } from "@/components/layout/page-header";
import { ThemePicker } from "@/components/appearance/theme-picker";
import { useTheme } from "@/lib/theme-context";

/**
 * Appearance settings — the "Themes" surface. Lets the user preview and apply
 * any registered theme with one click; the choice is persisted to
 * localStorage and re-applied on the next visit (see the theme engine in
 * `client/src/lib/theme.ts`).
 */
export default function AppearancePage() {
  const { theme } = useTheme();

  return (
    <div className="space-y-8">
      <PageHeader
        title="Appearance"
        description="Choose a theme for your workspace. Your selection is saved to this device and applied instantly."
        icon={Palette}
      />

      <div className="glass rounded-2xl px-4 py-3 text-sm text-muted">
        Active theme:{" "}
        <span className="font-medium text-foreground">{theme.name}</span>
        <span className="text-subtle"> · {theme.category}</span>
      </div>

      <ThemePicker />
    </div>
  );
}
