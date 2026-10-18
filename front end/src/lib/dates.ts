
/** "2026-10-20T14:30" (browser local time) -> "2026-10-20T11:30:00.000Z" */
export function localInputToUtcIso(value: string): string | null {
  if (!value) return null;
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? null : d.toISOString();
}

/** "2026-10-20T11:30:00Z" -> "2026-10-20T14:30" for the input */
export function utcIsoToLocalInput(iso: string | null): string {
  if (!iso) return '';
  const d = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString();
}