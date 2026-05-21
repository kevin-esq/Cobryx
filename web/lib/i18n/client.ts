import { defaultLocale, isLocale, LOCALE_COOKIE, type Locale } from "./config";

const COOKIE_MAX_AGE_SECONDS = 60 * 60 * 24 * 365;

function isBrowser(): boolean {
  return typeof document !== "undefined";
}

function readCookie(name: string): string | undefined {
  if (!isBrowser()) return undefined;
  const cookies = document.cookie ? document.cookie.split("; ") : [];
  for (const cookie of cookies) {
    const [rawKey, ...rest] = cookie.split("=");
    if (rawKey === name) {
      return decodeURIComponent(rest.join("="));
    }
  }
  return undefined;
}

export function getClientLocale(): Locale {
  const value = readCookie(LOCALE_COOKIE);
  return isLocale(value) ? value : defaultLocale;
}

export function setClientLocale(locale: Locale): void {
  if (!isBrowser()) return;
  document.cookie = [
    `${LOCALE_COOKIE}=${encodeURIComponent(locale)}`,
    "path=/",
    `max-age=${COOKIE_MAX_AGE_SECONDS}`,
    "samesite=lax"
  ].join("; ");
}
