// src/lib/formErrors.ts
import type { FieldValues, Path, UseFormSetError } from "react-hook-form";
import { ApiError } from "../api/client";

export function applyApiErrors<T extends FieldValues>(
  err: unknown,
  setError: UseFormSetError<T>,
  fields: readonly string[],
  conflictField?: Path<T>,
): string | null {
  if (!(err instanceof ApiError))
    return "Something went wrong. Please try again.";

  let handled = false;
  for (const [key, messages] of Object.entries(err.errors ?? {})) {
    if (fields.includes(key)) {
      setError(key as Path<T>, { type: "server", message: messages[0] });
      handled = true;
    }
  }
  if (err.status === 409 && conflictField) {
    setError(conflictField, { type: "server", message: err.message });
    handled = true;
  }
  return handled ? null : err.message;
}
