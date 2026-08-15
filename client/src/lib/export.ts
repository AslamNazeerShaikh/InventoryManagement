/**
 * First-party CSV export + browser download — no third-party dependency
 * (uses the Blob + Object URL Web APIs). RFC 4180 quoting so values with
 * commas, quotes or newlines survive round-trips through Excel/Sheets.
 */

export interface CsvColumn<T> {
  header: string;
  value: (row: T) => string | number | null | undefined;
}

function escapeCsvCell(value: string | number | null | undefined): string {
  const s = value == null ? "" : String(value);
  return /[",\r\n]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
}

export function toCsv<T>(rows: T[], columns: CsvColumn<T>[]): string {
  const header = columns.map((c) => escapeCsvCell(c.header)).join(",");
  const body = rows
    .map((row) => columns.map((c) => escapeCsvCell(c.value(row))).join(","))
    .join("\r\n");
  return body ? `${header}\r\n${body}` : header;
}

/** Trigger a client-side download of the given text as a file. */
export function downloadText(
  filename: string,
  text: string,
  mime = "text/csv;charset=utf-8;",
): void {
  if (typeof window === "undefined") return;
  // Prepend a UTF-8 BOM so Excel detects the encoding correctly.
  const blob = new Blob(["\uFEFF", text], { type: mime });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = filename;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}

/** Convenience: build + download a CSV in one call. */
export function exportCsv<T>(
  filename: string,
  rows: T[],
  columns: CsvColumn<T>[],
): void {
  downloadText(filename, toCsv(rows, columns));
}

/** A filesystem-friendly, timestamped filename like `inventory-20260815-1430.csv`. */
export function timestampedName(base: string, ext = "csv"): string {
  const d = new Date();
  const p = (n: number) => String(n).padStart(2, "0");
  return `${base}-${d.getFullYear()}${p(d.getMonth() + 1)}${p(d.getDate())}-${p(
    d.getHours(),
  )}${p(d.getMinutes())}.${ext}`;
}
