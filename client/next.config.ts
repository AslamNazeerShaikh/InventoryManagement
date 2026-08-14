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

const nextConfig: NextConfig = {
  async rewrites() {
    return [
      { source: "/api/health", destination: `${API_TARGET}/health` },
      { source: "/api/:path*", destination: `${API_TARGET}/api/:path*` },
    ];
  },
};

export default nextConfig;
