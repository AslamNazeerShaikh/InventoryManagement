/**
 * Global app configuration.
 *
 * By default the API base is empty, which means requests hit the same origin
 * (`/api/...`) and are transparently proxied to the ASP.NET API by the Next
 * rewrites in `next.config.ts`. Set `NEXT_PUBLIC_API_BASE_URL` to talk to a
 * remote API directly instead.
 */
export const API_BASE_URL = (
  process.env.NEXT_PUBLIC_API_BASE_URL ?? ""
).replace(/\/$/, "");

export const APP_NAME = "MediStock";
export const APP_TAGLINE = "Inventory Intelligence";
export const APP_DESCRIPTION =
  "Medical equipment inventory, expiry tracking and assignment workflows for healthcare teams.";

/** localStorage keys for the persisted session. */
export const STORAGE_KEYS = {
  accessToken: "ms.accessToken",
  refreshToken: "ms.refreshToken",
  expiresAt: "ms.expiresAt",
  user: "ms.user",
} as const;
