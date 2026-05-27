import { BACKEND_REFRESH_COOKIE } from "./constants";

/** Extract refreshToken value from Set-Cookie header(s) emitted by Cobryx.Api. */
export const parseRefreshTokenFromSetCookie = (
  setCookie: string | null
): { value: string; expires?: Date } | null => {
  if (!setCookie) return null;

  const segments = setCookie.split(/,(?=\s*[\w-]+=)/);
  for (const segment of segments) {
    const trimmed = segment.trim();
    if (!trimmed.startsWith(`${BACKEND_REFRESH_COOKIE}=`)) continue;

    const [pair, ...attrs] = trimmed.split(";");
    if (!pair) continue;
    const value = pair.slice(`${BACKEND_REFRESH_COOKIE}=`.length);
    if (!value) return null;

    let expires: Date | undefined;
    for (const attr of attrs) {
      const lower = attr.trim().toLowerCase();
      if (lower.startsWith("expires=")) {
        const parsed = new Date(attr.trim().slice("expires=".length));
        if (!Number.isNaN(parsed.getTime())) expires = parsed;
      }
    }

    return { value, expires };
  }

  return null;
};

export const collectSetCookieHeader = (response: Response): string | null => {
  const getSetCookie = (
    response.headers as Headers & { getSetCookie?: () => string[] }
  ).getSetCookie;

  if (typeof getSetCookie === "function") {
    const values = getSetCookie.call(response.headers);
    return values.length > 0 ? values.join(", ") : null;
  }

  return response.headers.get("set-cookie");
};
