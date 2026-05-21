import {
  defaultThemePreference,
  isThemePreference,
  THEME_COOKIE,
  type ResolvedTheme,
  type ThemePreference
} from "./config";

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

export function getClientThemePreference(): ThemePreference {
  const value = readCookie(THEME_COOKIE);
  return isThemePreference(value) ? value : defaultThemePreference;
}

export function setClientThemePreference(preference: ThemePreference): void {
  if (!isBrowser()) return;
  document.cookie = [
    `${THEME_COOKIE}=${encodeURIComponent(preference)}`,
    "path=/",
    `max-age=${COOKIE_MAX_AGE_SECONDS}`,
    "samesite=lax"
  ].join("; ");
}

export function prefersDarkColorScheme(): boolean {
  if (typeof window === "undefined" || !window.matchMedia) return false;
  return window.matchMedia("(prefers-color-scheme: dark)").matches;
}

export function applyThemeClass(theme: ResolvedTheme): void {
  if (!isBrowser()) return;
  const root = document.documentElement;
  if (theme === "dark") {
    root.classList.add("dark");
  } else {
    root.classList.remove("dark");
  }
  root.style.colorScheme = theme;
}
