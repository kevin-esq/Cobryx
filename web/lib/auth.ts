const ACCESS_TOKEN_KEY = "cobryx.access_token";

const isBrowser = (): boolean => typeof window !== "undefined";

export function getAccessToken(): string | null {
  if (!isBrowser()) {
    return null;
  }
  try {
    return window.localStorage.getItem(ACCESS_TOKEN_KEY);
  } catch {
    return null;
  }
}

export function setAccessToken(token: string): void {
  if (!isBrowser()) {
    return;
  }
  try {
    window.localStorage.setItem(ACCESS_TOKEN_KEY, token);
  } catch {
    // Silently ignore storage failures (private mode, quota, etc.).
  }
}

export function clearAccessToken(): void {
  if (!isBrowser()) {
    return;
  }
  try {
    window.localStorage.removeItem(ACCESS_TOKEN_KEY);
  } catch {
    // Silently ignore storage failures.
  }
}
