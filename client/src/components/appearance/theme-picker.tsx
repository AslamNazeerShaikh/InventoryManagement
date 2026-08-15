"use client";

import { motion } from "motion/react";
import { Check, Moon, Sun } from "lucide-react";
import { useTheme } from "@/lib/theme-context";
import {
  THEME_CATEGORIES,
  type ThemeCategory,
  type ThemeDefinition,
} from "@/lib/theme";
import { cn } from "@/lib/utils";

/**
 * Theme gallery. Renders every registered theme grouped by category with a
 * live colour preview; a single click applies and persists the theme. Because
 * the picker is driven entirely by the {@link THEMES} registry, new themes
 * appear here automatically.
 */
export function ThemePicker() {
  const { themeId, setThemeId, themes } = useTheme();

  return (
    <div className="space-y-9">
      {THEME_CATEGORIES.map((category) => {
        const inCategory = themes.filter((t) => t.category === category);
        if (inCategory.length === 0) return null;
        return (
          <section key={category} className="space-y-3.5">
            <div className="flex items-center gap-3">
              <h2 className="text-sm font-semibold uppercase tracking-wider text-muted">
                {category}
              </h2>
              <span className="h-px flex-1 bg-line" />
            </div>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
              {inCategory.map((theme) => (
                <ThemeCard
                  key={theme.id}
                  theme={theme}
                  selected={theme.id === themeId}
                  onSelect={() => setThemeId(theme.id)}
                />
              ))}
            </div>
          </section>
        );
      })}
    </div>
  );
}

function ThemeCard({
  theme,
  selected,
  onSelect,
}: {
  theme: ThemeDefinition;
  selected: boolean;
  onSelect: () => void;
}) {
  const { bg, surface, accent, text } = theme.swatches;
  const ModeIcon = theme.mode === "dark" ? Moon : Sun;

  return (
    <motion.button
      type="button"
      onClick={onSelect}
      whileHover={{ y: -3 }}
      whileTap={{ scale: 0.98 }}
      transition={{ type: "spring", stiffness: 400, damping: 28 }}
      aria-pressed={selected}
      aria-label={`Apply ${theme.name} theme`}
      className={cn(
        "group card-hover relative overflow-hidden rounded-2xl border p-3 text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400/60",
        selected
          ? "border-brand-400/60 shadow-glow"
          : "border-line hover:border-line-strong",
      )}
    >
      {/* Live colour preview built from the theme's own swatches. */}
      <div
        className="relative aspect-[16/10] w-full overflow-hidden rounded-xl ring-1 ring-black/5"
        style={{ backgroundColor: bg }}
      >
        <div
          className="absolute inset-x-3 top-3 flex items-center gap-1.5 rounded-lg px-2.5 py-2"
          style={{ backgroundColor: surface }}
        >
          <span
            className="size-2 rounded-full"
            style={{ backgroundColor: accent }}
          />
          <span
            className="h-1.5 w-10 rounded-full"
            style={{ backgroundColor: text, opacity: 0.28 }}
          />
          <span
            className="ml-auto h-4 w-9 rounded-md"
            style={{ backgroundColor: accent }}
          />
        </div>
        <div className="absolute inset-x-3 bottom-3 space-y-1.5">
          <span
            className="block h-1.5 w-3/4 rounded-full"
            style={{ backgroundColor: text, opacity: 0.55 }}
          />
          <span
            className="block h-1.5 w-1/2 rounded-full"
            style={{ backgroundColor: text, opacity: 0.28 }}
          />
        </div>
      </div>

      <div className="mt-3 flex items-center justify-between gap-2 px-0.5">
        <div className="min-w-0">
          <p className="truncate text-sm font-semibold text-foreground">
            {theme.name}
          </p>
          <p className="mt-0.5 flex items-center gap-1 text-xs text-subtle">
            <ModeIcon className="size-3" />
            {theme.mode === "dark" ? "Dark" : "Light"}
          </p>
        </div>
        <span
          className={cn(
            "flex size-6 shrink-0 items-center justify-center rounded-full transition",
            selected
              ? "bg-brand-500 text-on-accent"
              : "bg-overlay text-transparent group-hover:text-muted",
          )}
        >
          <Check className="size-3.5" strokeWidth={3} />
        </span>
      </div>
    </motion.button>
  );
}
