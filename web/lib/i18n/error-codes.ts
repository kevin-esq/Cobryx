import type { Dictionary } from "./dictionaries";
import { interpolate, type InterpolationVars } from "./format";

/**
 * Shape of an error coming from `apiFetch` (lib/api-client.ts) plus any
 * other transport-level error we may surface. Defensive on purpose:
 * the back-end contract may evolve, and we don't want UI code to crash.
 */
export interface ApiErrorShape {
  status?: number;
  body?: unknown;
  message?: string;
  name?: string;
}

/**
 * Extracts a code like `AUTH.LOGIN.INVALID_CREDENTIALS` from common API
 * problem-details shapes:
 * - { code: "..." }
 * - { errorCode: "..." }
 * - { type: "..." }           ← RFC 7807 type URN
 * - { extensions: { code } }  ← ASP.NET ProblemDetails extensions
 * - { errors: [{ code }] }    ← FluentValidation-ish lists
 */
export function getApiErrorCode(error: unknown): string | undefined {
  if (!error || typeof error !== "object") return undefined;

  const body = (error as ApiErrorShape).body;
  const candidates: unknown[] = [
    isRecord(body) ? body.code : undefined,
    isRecord(body) ? body.errorCode : undefined,
    isRecord(body) ? body.outcomeCode : undefined,
    isRecord(body) ? body.type : undefined,
    isRecord(body) && isRecord(body.extensions) ? body.extensions.code : undefined,
    isRecord(body) && Array.isArray(body.errors) && isRecord(body.errors[0])
      ? body.errors[0].code
      : undefined
  ];

  for (const candidate of candidates) {
    if (typeof candidate === "string" && candidate.length > 0) {
      return candidate.toUpperCase();
    }
  }

  return undefined;
}

export interface TranslatedApiError {
  code: string | null;
  message: string;
  status?: number;
}

/**
 * Translate an API error into a user-facing message using the active dictionary.
 *
 * Resolution order:
 * 1. Explicit code present in `dictionary.apiErrors` (e.g. AUTH.LOGIN.INVALID_CREDENTIALS).
 * 2. Generic bucket inferred from HTTP status (401, 403, 404, 422, 429, 5xx).
 * 3. Network / unknown -> `errors.fallback`.
 */
export function translateApiError(
  error: unknown,
  dictionary: Dictionary,
  vars?: InterpolationVars
): TranslatedApiError {
  const code = getApiErrorCode(error) ?? null;
  const status = isRecord(error) && typeof error.status === "number" ? error.status : undefined;

  if (code) {
    const entry = (dictionary.apiErrors as Record<string, string | undefined>)[code];
    if (entry) {
      return { code, message: interpolate(entry, vars), status };
    }
  }

  const fallbackMessage = pickByStatus(status, dictionary);
  return { code, message: interpolate(fallbackMessage, vars), status };
}

function pickByStatus(status: number | undefined, dictionary: Dictionary): string {
  if (status == null) return dictionary.errors.network;
  if (status === 401) return dictionary.errors.unauthorized;
  if (status === 403) return dictionary.errors.forbidden;
  if (status === 404) return dictionary.errors.notFound;
  if (status === 422) return dictionary.errors.validation;
  if (status === 429) return dictionary.errors.rateLimited;
  if (status >= 500) return dictionary.errors.serverError;
  return dictionary.errors.fallback;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === "object";
}
