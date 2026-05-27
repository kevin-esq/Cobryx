import type { SessionPayload } from "./types";

export const isSessionExpired = (session: SessionPayload): boolean => {
  const expires = new Date(session.expiresAt).getTime();
  return Number.isNaN(expires) || expires <= Date.now() + 30_000;
};
