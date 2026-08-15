"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import {
  DEFAULT_THEME_ID,
  THEMES,
  applyThemeId,
  getStoredThemeId,
  getTheme,
  persistThemeId,
  type ThemeDefinition,
} from "@/lib/theme";

interface ThemeContextValue {
  /** The active theme id. */
  themeId: string;
  /** The active theme definition (metadata + swatches). */
  theme: ThemeDefinition;
  /** All available themes, in catalogue order. */
  themes: readonly ThemeDefinition[];
  /** Apply + persist a theme by id (no-op for unknown ids). */
  setThemeId: (id: string) => void;
}

const ThemeContext = createContext<ThemeContextValue | null>(null);

/**
 * Provides the current theme and a one-call setter. The actual first-paint
 * application is done by the inline boot script in the root layout
 * (`THEME_INIT_SCRIPT`); this provider only mirrors that into React state and
 * handles subsequent user-driven changes, so there is no theme flash.
 */
export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const [themeId, setThemeIdState] = useState<string>(DEFAULT_THEME_ID);

  // Mirror whatever the no-FOUC script already applied to <html> into state.
  useEffect(() => {
    const stored = getStoredThemeId();
    if (getTheme(stored)) setThemeIdState(stored as string);
  }, []);

  const setThemeId = useCallback((id: string) => {
    if (!getTheme(id)) return;
    applyThemeId(id);
    persistThemeId(id);
    setThemeIdState(id);
  }, []);

  const value = useMemo<ThemeContextValue>(
    () => ({
      themeId,
      theme: getTheme(themeId) ?? getTheme(DEFAULT_THEME_ID)!,
      themes: THEMES,
      setThemeId,
    }),
    [themeId, setThemeId],
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

/** Access the active theme and switcher. Must be used within `ThemeProvider`. */
export function useTheme(): ThemeContextValue {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error("useTheme must be used within a ThemeProvider");
  return ctx;
}
