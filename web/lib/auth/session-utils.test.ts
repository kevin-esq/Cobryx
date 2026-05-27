import { describe, expect, it } from "vitest";

import { isSessionExpired } from "./session-utils";

describe("isSessionExpired", () => {
  it("returns false when session expires more than 30s in the future", () => {
    const session = {
      accessToken: "token",
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      user: { email: "a@b.com", firstName: null, lastName: null, fullName: null, role: null }
    };
    expect(isSessionExpired(session)).toBe(false);
  });

  it("returns true when session is already expired", () => {
    const session = {
      accessToken: "token",
      expiresAt: new Date(Date.now() - 1_000).toISOString(),
      user: { email: "a@b.com", firstName: null, lastName: null, fullName: null, role: null }
    };
    expect(isSessionExpired(session)).toBe(true);
  });
});
