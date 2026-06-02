import { describe, expect, it } from "vitest";

import { parseRefreshTokenFromSetCookie } from "./cookies";

describe("parseRefreshTokenFromSetCookie", () => {
  it("extracts refresh token value and expiry from a single Set-Cookie header", () => {
    const header =
      "refreshToken=abc123; path=/; httponly; expires=Wed, 28 May 2026 12:00:00 GMT; samesite=strict";

    const parsed = parseRefreshTokenFromSetCookie(header);
    expect(parsed?.value).toBe("abc123");
    expect(parsed?.expires).toBeInstanceOf(Date);
  });

  it("finds refreshToken among multiple cookies", () => {
    const header =
      "other=value; Path=/, refreshToken=rotated; Path=/; HttpOnly; SameSite=Strict";

    const parsed = parseRefreshTokenFromSetCookie(header);
    expect(parsed?.value).toBe("rotated");
  });

  it("returns null when refresh token cookie is absent", () => {
    expect(parseRefreshTokenFromSetCookie("session=foo; HttpOnly")).toBeNull();
    expect(parseRefreshTokenFromSetCookie(null)).toBeNull();
  });
});
