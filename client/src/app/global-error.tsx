"use client";

import { useEffect } from "react";

// Last-resort boundary for errors thrown in the root layout. It replaces the
// whole document, so it renders its own <html>/<body> with self-contained
// inline styles (no dependency on the app CSS that may have failed to load).
export default function GlobalError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <html lang="en">
      <body
        style={{
          margin: 0,
          minHeight: "100vh",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          background: "#05070e",
          color: "#e7ecf6",
          fontFamily: "system-ui, -apple-system, Segoe UI, Roboto, sans-serif",
        }}
      >
        <div
          style={{
            maxWidth: 420,
            padding: 32,
            textAlign: "center",
            border: "1px solid rgba(255,255,255,0.1)",
            borderRadius: 16,
            background: "rgba(255,255,255,0.03)",
          }}
        >
          <h1 style={{ fontSize: 18, fontWeight: 600, margin: 0 }}>
            Application error
          </h1>
          <p style={{ marginTop: 8, fontSize: 14, color: "#9aa7b8" }}>
            A critical error occurred. Please reload the page.
          </p>
          <button
            onClick={() => reset()}
            style={{
              marginTop: 24,
              padding: "10px 16px",
              fontSize: 14,
              fontWeight: 600,
              color: "#05070e",
              background: "#12b88b",
              border: "none",
              borderRadius: 12,
              cursor: "pointer",
            }}
          >
            Reload
          </button>
        </div>
      </body>
    </html>
  );
}
