/**
 * Formats an error thrown by a mutation into a message fit to show the
 * user. `api/hooks.ts`'s `unwrap()` throws the REST API's own JSON error
 * body on a non-2xx response — a FastAPI `HTTPException(status, detail)`
 * response is `{ detail: "some message" }`, and a Pydantic validation
 * failure (422) is `{ detail: [{ loc, msg, type }, ...] }`, one entry per
 * invalid field. Reads whichever shape actually came back rather than
 * showing the caller's own generic guess regardless of the real cause —
 * a bare `catch { setError("...") }` shows the same fixed text whether
 * the real problem was a validation error, a permission check, or
 * something else entirely, which is exactly what made an earlier version
 * of this message ("check required fields") actively misleading for a
 * failure that had nothing to do with required fields.
 */
export function formatApiError(err: unknown, fallback: string): string {
  const detail = (err as { detail?: unknown } | null | undefined)?.detail;
  if (typeof detail === "string" && detail) return detail;
  if (Array.isArray(detail) && detail.length > 0) {
    return detail
      .map((item) => {
        const loc = Array.isArray((item as { loc?: unknown[] })?.loc)
          ? (item as { loc: unknown[] }).loc
          : [];
        // Pydantic's loc is ["body", "fieldName", ...] for a request-body
        // field — drop the leading "body" so the message just names the field.
        const field = loc.filter((p) => p !== "body").join(".");
        const msg = (item as { msg?: string }).msg ?? "invalid value";
        return field ? `${field}: ${msg}` : msg;
      })
      .join("; ");
  }
  return fallback;
}
