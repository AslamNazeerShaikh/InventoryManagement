import type { NextConfig } from "next";

/**
 * The Next dev server proxies /api/* to the ASP.NET Core API, so the browser
 * always stays same-origin (no CORS, no mixed-content, no certificate prompts).
 *
 * We target the API's HTTP endpoint (default http://localhost:5050). Run the API
 * with the "http" launch profile so its HTTPS redirect stays a no-op. Override
 * the target with the API_PROXY_TARGET environment variable if needed.
 */
const API_TARGET = process.env.API_PROXY_TARGET ?? "http://localhost:5050";

const isDev = process.env.NODE_ENV !== "production";

// Content-Security-Policy. Dev needs 'unsafe-eval' + ws: for Turbopack HMR and
// relaxes frame-ancestors so an editor browser preview still works; prod is
// locked down. Inline scripts/styles are required by Next hydration + motion.
const csp = [
  "default-src 'self'",
  `script-src 'self' 'unsafe-inline'${isDev ? " 'unsafe-eval'" : ""}`,
  "style-src 'self' 'unsafe-inline'",
  "img-src 'self' data: blob:",
  "font-src 'self' data:",
  `connect-src 'self'${isDev ? " ws: wss:" : ""}`,
  `frame-ancestors ${isDev ? "'self'" : "'none'"}`,
  "base-uri 'self'",
  "form-action 'self'",
  "object-src 'none'",
].join("; ");

const securityHeaders = [
  { key: "Content-Security-Policy", value: csp },
  { key: "X-Content-Type-Options", value: "nosniff" },
  { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
  { key: "Permissions-Policy", value: "camera=(), microphone=(), geolocation=()" },
  { key: "X-DNS-Prefetch-Control", value: "off" },
  ...(isDev
    ? []
    : [
        { key: "X-Frame-Options", value: "DENY" },
        {
          key: "Strict-Transport-Security",
          value: "max-age=63072000; includeSubDomains; preload",
        },
      ]),
];

const nextConfig: NextConfig = {
  poweredByHeader: false,
  reactStrictMode: true,
  async headers() {
    return [{ source: "/:path*", headers: securityHeaders }];
  },
  async rewrites() {
    return [
      { source: "/api/health", destination: `${API_TARGET}/health` },
      { source: "/api/:path*", destination: `${API_TARGET}/api/:path*` },
    ];
  },
};

export default nextConfig;
