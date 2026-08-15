/**
 * Theme engine — the single source of truth for the app's colour themes.
 *
 * Adding a new theme is a two-step, self-contained change:
 *   1. Add a `[data-theme="<id>"]` block in `globals.css` that sets the ~20
 *      base variables (surface/foreground/brand/aurora/…).
 *   2. Append a {@link ThemeDefinition} entry below with the same `id`.
 * Nothing else needs to change: the picker, the no-FOUC boot script and the
 * `<Toaster>` all derive from this registry, so the feature is extensible by
 * design.
 *
 * How theming works at runtime: setting `data-theme` on `<html>` swaps the CSS
 * variables; because every Tailwind colour utility resolves to `var(--color-*)`
 * (and legacy literals like `text-white`/`bg-ink-800` are aliased to the
 * semantic tokens in `globals.css`), the whole UI re-skins with no per-render
 * cost. See `client/src/app/globals.css` for the token definitions.
 */

export type ThemeMode = "dark" | "light";

export type ThemeCategory = "Modern" | "Aesthetic" | "Futuristic" | "Simple";

/** Small preview colours used by the Appearance picker cards. */
export interface ThemeSwatches {
  bg: string;
  surface: string;
  accent: string;
  text: string;
}

export interface ThemeDefinition {
  /** Stable id; matches the `[data-theme="…"]` selector in globals.css. */
  id: string;
  name: string;
  description: string;
  category: ThemeCategory;
  mode: ThemeMode;
  swatches: ThemeSwatches;
}

/** The ordered list of categories shown in the picker. */
export const THEME_CATEGORIES: ThemeCategory[] = [
  "Modern",
  "Aesthetic",
  "Futuristic",
  "Simple",
];

/**
 * The theme catalogue. `midnight` is the default (rendered by `:root`).
 * Keep this in sync with the `[data-theme]` blocks in globals.css.
 */
export const THEMES: readonly ThemeDefinition[] = [
  {
    id: "midnight",
    name: "Midnight",
    description: "Deep slate with a fresh teal accent — the signature look.",
    category: "Modern",
    mode: "dark",
    swatches: { bg: "#05070e", surface: "#0d1322", accent: "#12b88b", text: "#e7ecf6" },
  },
  {
    id: "daylight",
    name: "Daylight",
    description: "Clean, bright workspace with a calm teal accent.",
    category: "Modern",
    mode: "light",
    swatches: { bg: "#f3f6fb", surface: "#ffffff", accent: "#14b8a6", text: "#0f1b2d" },
  },
  {
    id: "sunset",
    name: "Sunset",
    description: "Warm charcoal washed with amber and rose.",
    category: "Aesthetic",
    mode: "dark",
    swatches: { bg: "#140a06", surface: "#241310", accent: "#f97316", text: "#fbeee6" },
  },
  {
    id: "linen",
    name: "Linen",
    description: "Soft warm paper with a terracotta accent.",
    category: "Aesthetic",
    mode: "light",
    swatches: { bg: "#faf5ee", surface: "#fffdf9", accent: "#f97316", text: "#2a1c10" },
  },
  {
    id: "nebula",
    name: "Nebula",
    description: "Neon indigo and cyan on cosmic violet.",
    category: "Futuristic",
    mode: "dark",
    swatches: { bg: "#08061a", surface: "#120f2e", accent: "#6366f1", text: "#ece9ff" },
  },
  {
    id: "arctic",
    name: "Arctic",
    description: "Crisp, cool light with an electric blue accent.",
    category: "Futuristic",
    mode: "light",
    swatches: { bg: "#eef4fb", surface: "#ffffff", accent: "#2563eb", text: "#0b1b33" },
  },
  {
    id: "graphite",
    name: "Graphite",
    description: "Understated neutral dark with a sky-blue highlight.",
    category: "Simple",
    mode: "dark",
    swatches: { bg: "#0b0e13", surface: "#151a22", accent: "#0ea5e9", text: "#e8edf3" },
  },
  {
    id: "paper",
    name: "Paper",
    description: "Minimal light-grey canvas with an indigo accent.",
    category: "Simple",
    mode: "light",
    swatches: { bg: "#f6f7f9", surface: "#ffffff", accent: "#4f46e5", text: "#111827" },
  },
];

export const DEFAULT_THEME_ID = "midnight";

/** localStorage key (kept in the `ms.` namespace used across the app). */
export const THEME_STORAGE_KEY = "ms.theme";

const THEME_IDS = THEMES.map((t) => t.id);
const DARK_THEME_IDS = THEMES.filter((t) => t.mode === "dark").map((t) => t.id);

const THEME_BY_ID = new Map(THEMES.map((t) => [t.id, t]));

/** Returns the theme with the given id, if it exists. */
export function getTheme(id: string | null | undefined): ThemeDefinition | undefined {
  return id ? THEME_BY_ID.get(id) : undefined;
}

/** Type-guard: is this string a known theme id? */
export function isThemeId(id: string | null | undefined): id is string {
  return !!id && THEME_BY_ID.has(id);
}

/**
 * Applies a theme to the document by setting `data-theme` (and the matching
 * `color-scheme`) on `<html>`. Safe to call only in the browser.
 */
export function applyThemeId(id: string): void {
  if (typeof document === "undefined") return;
  const theme = getTheme(id) ?? getTheme(DEFAULT_THEME_ID)!;
  const root = document.documentElement;
  root.setAttribute("data-theme", theme.id);
  root.style.colorScheme = theme.mode;
}

/** Persists the chosen theme id; never throws (e.g. private-mode storage). */
export function persistThemeId(id: string): void {
  try {
    localStorage.setItem(THEME_STORAGE_KEY, id);
  } catch {
    /* ignore storage failures */
  }
}

/** Reads the persisted theme id, or null when unset/unavailable. */
export function getStoredThemeId(): string | null {
  try {
    return localStorage.getItem(THEME_STORAGE_KEY);
  } catch {
    return null;
  }
}

/**
 * Inline script injected into `<head>` to apply the saved theme *before* first
 * paint, eliminating a flash of the wrong theme (FOUC). It is intentionally
 * dependency-free and derived from this registry so it never drifts.
 */
export const THEME_INIT_SCRIPT = `(function(){try{var k=${JSON.stringify(
  THEME_STORAGE_KEY,
)},d=${JSON.stringify(DEFAULT_THEME_ID)},v=${JSON.stringify(
  THEME_IDS,
)},dk=${JSON.stringify(
  DARK_THEME_IDS,
)};var t=localStorage.getItem(k)||d;if(v.indexOf(t)<0)t=d;var e=document.documentElement;e.setAttribute("data-theme",t);e.style.colorScheme=dk.indexOf(t)>=0?"dark":"light";}catch(_){}})();`;
