# Theme Engine & UI Polish — Change Log

**Scope:** Cosmetic (UI/UX) only. No backend, API, DTO, or ABI changes. All
edits are confined to `client/` (the Next.js frontend).

**Goal (from request):** keep the translucent "glass" look but make it readable;
unify shadows, outlines, padding and popup/scrollbar styling; make the UI
responsive from 720p to 4K in portrait and landscape; and add an **extensible
theme engine** with a one-click palette of light & dark themes grouped into
*Modern / Aesthetic / Futuristic / Simple* categories.

---

## 1. Mind map

```mermaid
flowchart TD
  A[globals.css<br/>design tokens] -->|semantic vars| B[Tailwind v4 @theme]
  A -->|:root + data-theme blocks| C[8 themes<br/>4 categories x light/dark]
  A -->|legacy aliases| D[existing components<br/>re-skin automatically]
  E[lib/theme.ts<br/>registry + apply + boot script] --> F[lib/theme-context.tsx<br/>ThemeProvider + useTheme]
  F --> G[providers.tsx<br/>ThemedToaster]
  E --> H[layout.tsx<br/>no-FOUC inline script]
  F --> I[appearance/theme-picker.tsx]
  I --> J[app/(app)/appearance/page.tsx]
  K[nav.ts / user-menu.tsx] -->|links| J
```

**Key idea (why this is cheap & safe):** In Tailwind CSS v4 every colour utility
compiles to `var(--color-*)`. By (a) adding *semantic* tokens
(`surface`, `foreground`, `muted`, `overlay`, `line`, `on-accent`, …) and
(b) **aliasing the legacy palette** the app already uses
(`--color-white`, `--color-slate-*`, `--color-ink-*`, `--color-brand-*`) to those
semantic tokens per `[data-theme]`, switching a single `data-theme` attribute on
`<html>` re-skins **every existing component** — light or dark — with no
component rewrites and no runtime cost.

---

## 2. Files changed / added

### Added

| File | Purpose |
| --- | --- |
| [client/src/lib/theme.ts](../client/src/lib/theme.ts) | Framework-agnostic theme engine: the `THEMES` registry (single source of truth), `applyThemeId`/`persistThemeId`/`getStoredThemeId`, and the dependency-free `THEME_INIT_SCRIPT` used to prevent a flash of the wrong theme. |
| [client/src/lib/theme-context.tsx](../client/src/lib/theme-context.tsx) | `ThemeProvider` + `useTheme()` hook. Mirrors the booted theme into React state and applies/persists user changes. |
| [client/src/components/appearance/theme-picker.tsx](../client/src/components/appearance/theme-picker.tsx) | The gallery: theme cards grouped by category with a live colour preview and one-click apply. Driven entirely by the registry, so new themes appear automatically. |
| [client/src/app/(app)/appearance/page.tsx](../client/src/app/(app)/appearance/page.tsx) | The "Appearance" settings screen. |
| [docs/Theme-UI-Changes.md](Theme-UI-Changes.md) | This document. |

### Modified

| File | What & why |
| --- | --- |
| [client/src/app/globals.css](../client/src/app/globals.css) | Rewritten into a token-driven design system (see §3). Adds 8 `[data-theme]` blocks, semantic `@theme` tokens, legacy-palette aliases, more-opaque (readable) `glass`/`panel`, themed scrollbars/selection/aurora, `shadow-glow`/`shadow-panel` utilities, and 4K rem-scaling. |
| [client/src/app/layout.tsx](../client/src/app/layout.tsx) | Injects `THEME_INIT_SCRIPT` in `<head>` and adds `suppressHydrationWarning` on `<html>` (the boot script mutates `data-theme` before hydration). |
| [client/src/components/providers.tsx](../client/src/components/providers.tsx) | Wraps the tree in `ThemeProvider`; the toast layer (`ThemedToaster`) now follows the active light/dark mode and uses semantic tokens. |
| [client/src/components/layout/nav.ts](../client/src/components/layout/nav.ts) | Adds the **Appearance** nav item (visible to all roles). |
| [client/src/components/layout/user-menu.tsx](../client/src/components/layout/user-menu.tsx) | Adds a quick **Appearance** link in the account dropdown. |
| [client/src/components/ui/button.tsx](../client/src/components/ui/button.tsx) | `danger` variant text `text-white` → `text-on-accent` so it stays legible on its fixed red gradient under light themes. |
| [client/src/components/ui/switch.tsx](../client/src/components/ui/switch.tsx) | Toggle knob `bg-white` → `bg-[#ffffff]` (stays literally white in every theme rather than following the aliased foreground). |
| [client/src/components/ui/modal.tsx](../client/src/components/ui/modal.tsx) | Scrim `bg-ink-950/75` → `bg-backdrop` (proper dim under light themes); dividers → `border-line`; close-button hover → semantic tokens. |
| [client/src/components/ui/card.tsx](../client/src/components/ui/card.tsx) | Header/footer dividers → `border-line` to unify outlines with the rest of the system. |
| [client/src/components/layout/notifications.tsx](../client/src/components/layout/notifications.tsx) | Alert-count badge text `text-white` → `text-on-accent` (legible on its fixed rose badge under light themes). |

---

## 3. The design system (`globals.css`)

**How it is layered:**

1. **`@theme`** — declares the Tailwind tokens: semantic colours
   (`--color-surface/foreground/muted/subtle/overlay/overlay-strong/backdrop/on-accent/line/line-strong/ring`),
   the brand scale, fonts, radii and animations. This is what makes utilities
   such as `bg-surface`, `text-muted`, `border-line`, `text-on-accent` exist.
2. **`:root`** — the default **Midnight** theme's ~20 base variables
   (`--surface`, `--foreground`, `--brand-500`, `--aurora-*`, `--shadow-*`, …).
3. **`[data-theme="…"]`** — one block per additional theme overriding those base
   variables (7 blocks: nebula, sunset, graphite, daylight, arctic, linen, paper).
4. **`:root, [data-theme]`** (shared) — *derives* `--overlay`, `--line`, `--ring`,
   scrollbar colours from `--foreground`, and **aliases** the legacy palette
   (`--color-white`, `--color-slate-*`, `--color-ink-*`, `--color-brand-*`) onto
   the semantic tokens. This is the compatibility layer that re-skins the
   existing ~40 components for free.

**Readability fix (the "glass" ask):** `glass` now paints a 74 %-opaque surface
(was near-transparent) and `panel` 90 %, both keeping `backdrop-filter: blur()`.
The frosted look is preserved but text no longer fights the background.

**Consistency:** scrollbars, text selection, focus rings, the ambient aurora and
all elevation now read from theme variables, so popovers, dropdown menus, modals
and the sidebar share one coherent set of outlines and shadows. Native controls
(`<select>` menus, date pickers, scrollbars) follow the theme via `color-scheme`.

**Responsive / 4K:** the layout already uses fluid Tailwind breakpoints and
`max-w` containers; on top of that the root font-size steps up at
`≥2560px` (18px) and `≥3840px` (20px) so the rem-based UI scales up on QHD/4K
instead of looking tiny, in both orientations. A `prefers-reduced-motion` block
disables animation for users who request it.

---

## 4. Runtime flow (no flash of wrong theme)

1. On first request, the inline `THEME_INIT_SCRIPT` in `<head>` runs
   *synchronously before paint*, reads `localStorage["ms.theme"]`, validates it
   against the registry and sets `data-theme` + `color-scheme` on `<html>`.
2. React hydrates; `ThemeProvider` mirrors that value into state
   (`suppressHydrationWarning` avoids the expected attribute mismatch warning).
3. Picking a theme calls `setThemeId` → `applyThemeId` (swaps the attribute) +
   `persistThemeId` (saves to `localStorage`). No reload, no API call.

---

## 5. Extending the engine (add a theme in 2 steps)

1. Add a `[data-theme="<id>"] { … }` block in `globals.css` setting the ~20 base
   variables (copy an existing block of the same mode as a starting point).
2. Append a `ThemeDefinition` to `THEMES` in
   [client/src/lib/theme.ts](../client/src/lib/theme.ts) with the same `id`, a
   `category`, `mode` and preview `swatches`.

The picker, the boot script, the toast theming and persistence all derive from
the registry, so nothing else needs to change. (This satisfies the "global theme
engine / config so new themes can be added easily" requirement.)

**Current catalogue (8):**

| Category | Dark | Light |
| --- | --- | --- |
| Modern | Midnight (default) | Daylight |
| Aesthetic | Sunset | Linen |
| Futuristic | Nebula | Arctic |
| Simple | Graphite | Paper |

---

## 6. Accessibility & compatibility

- **Contrast:** foreground/muted/subtle tokens are tuned per theme for legible
  body and secondary text on each surface; fixed-colour affordances (danger
  button, alert badge) use `on-accent` so they stay readable in light themes.
- **Focus:** visible `ring-brand-400` focus states retained on interactive
  elements, including the theme cards.
- **Motion:** honours `prefers-reduced-motion`.
- **Colour-scheme:** set per theme so native form controls, scrollbars and
  caret colours match light/dark.
- **Cross-browser:** uses standard CSS custom properties, `color-mix()` and
  `backdrop-filter` (supported by current evergreen browsers); the app degrades
  gracefully (solid surfaces) if `backdrop-filter` is unavailable.

---

## 7. Verification performed

- `tsc --noEmit` on the client: **pass**; editor diagnostics: **clean**.
- Live (dev servers on :3000 / :5050): login, Appearance picker, Dashboard and
  Inventory verified in **Midnight (dark)** and **Daylight (light)** — full
  re-skin including tables, selects, badges and semantic status colours
  (e.g. amber expiry warnings) preserved.
- **Persistence + no-FOUC** confirmed across a hard reload.
- **Responsive** confirmed at 390 px (mobile: hamburger nav + stacked cards) and
  desktop widths.
