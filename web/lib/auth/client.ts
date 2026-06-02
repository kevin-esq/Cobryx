"use client";

import { apiFetch } from "@/lib/api-client";

export type LoginResult =
  | {
      kind: "authenticated";
      user: {
        email: string | null;
        firstName: string | null;
        lastName: string | null;
        fullName: string | null;
        role: string | null;
      };
      requiresOnboarding: boolean;
    }
  | { kind: "mfa"; mfaToken: string; requiresOnboarding: boolean };

type LoginApiResponse = {
  success: boolean;
  requiresMfa?: boolean;
  mfaToken?: string | null;
  requiresOnboarding?: boolean;
  user?: LoginResult extends { kind: "authenticated" } ? LoginResult["user"] : never;
};

export const login = async (email: string, password: string): Promise<LoginResult> => {
  const result = await apiFetch<LoginApiResponse>("/api/auth/login", {
    method: "POST",
    body: { email, password }
  });

  if (result.requiresMfa && result.mfaToken) {
    return {
      kind: "mfa",
      mfaToken: result.mfaToken,
      requiresOnboarding: result.requiresOnboarding ?? false
    };
  }

  if (!result.user) {
    throw new Error("Login succeeded but session payload is missing.");
  }

  return {
    kind: "authenticated",
    user: result.user,
    requiresOnboarding: result.requiresOnboarding ?? false
  };
};

export const verifyMfaTotp = async (
  mfaToken: string,
  code: string
): Promise<LoginResult> => {
  const result = await apiFetch<LoginApiResponse>("/api/auth/mfa/totp", {
    method: "POST",
    body: { persistenceToken: mfaToken, code }
  });

  if (!result.user) {
    throw new Error("MFA verification succeeded but session payload is missing.");
  }

  return {
    kind: "authenticated",
    user: result.user,
    requiresOnboarding: result.requiresOnboarding ?? false
  };
};

export const logout = async (): Promise<void> => {
  await apiFetch("/api/auth/logout", { method: "POST" });
};

export const revokeAllSessions = async (): Promise<void> => {
  await apiFetch("/api/auth/sessions/revoke-all", { method: "POST" });
};
