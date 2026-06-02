"use client";

import { apiFetch } from "@/lib/api-client";
import type { ApiEnvelope } from "@/lib/auth/types";
import type { LoginResult } from "@/lib/auth/client";

const base64UrlToBuffer = (value: string): ArrayBuffer => {
  const padded = value.replace(/-/g, "+").replace(/_/g, "/");
  const padLength = (4 - (padded.length % 4)) % 4;
  const base64 = padded + "=".repeat(padLength);
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i += 1) {
    bytes[i] = binary.charCodeAt(i);
  }
  return bytes.buffer;
};

const bufferToBase64Url = (buffer: ArrayBuffer): string => {
  const bytes = new Uint8Array(buffer);
  let binary = "";
  for (let i = 0; i < bytes.length; i += 1) {
    binary += String.fromCharCode(bytes[i]!);
  }
  return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
};

type Fido2ChallengeEnvelope = ApiEnvelope<Record<string, unknown>>;

const toPublicKeyOptions = (
  raw: Record<string, unknown>
): PublicKeyCredentialRequestOptions => {
  const allowCredentials = Array.isArray(raw.allowCredentials)
    ? raw.allowCredentials.map((cred) => {
        const item = cred as Record<string, unknown>;
        return {
          ...item,
          id: base64UrlToBuffer(String(item.id ?? "")),
          type: "public-key" as const
        };
      })
    : undefined;

  return {
    ...raw,
    challenge: base64UrlToBuffer(String(raw.challenge ?? "")),
    allowCredentials
  } as PublicKeyCredentialRequestOptions;
};

export const verifyMfaFido2 = async (mfaToken: string): Promise<LoginResult> => {
  const challengeEnvelope = await apiFetch<Fido2ChallengeEnvelope>(
    "/api/auth/mfa/fido2/challenge",
    {
      method: "POST",
      body: { persistenceToken: mfaToken }
    }
  );

  const optionsRaw = challengeEnvelope.data;
  if (!optionsRaw || typeof optionsRaw !== "object") {
    throw new Error("FIDO2 challenge options are missing from the API response.");
  }

  const publicKey = toPublicKeyOptions(optionsRaw);
  const assertion = (await navigator.credentials.get({
    publicKey
  })) as PublicKeyCredential | null;

  if (!assertion) {
    throw new Error("FIDO2 assertion was cancelled.");
  }

  const assertionResponse = assertion.response as AuthenticatorAssertionResponse;
  const clientDataJSON = bufferToBase64Url(assertionResponse.clientDataJSON);
  const authenticatorData = bufferToBase64Url(assertionResponse.authenticatorData);
  const signature = bufferToBase64Url(assertionResponse.signature);
  const userHandle = assertionResponse.userHandle
    ? bufferToBase64Url(assertionResponse.userHandle)
    : null;

  const verifyPayload = {
    persistenceToken: mfaToken,
    verificationData: {
      id: assertion.id,
      rawId: bufferToBase64Url(assertion.rawId),
      type: assertion.type,
      response: {
        clientDataJSON,
        authenticatorData,
        signature,
        userHandle
      },
      extensions: {}
    },
    challenge: {
      status: "OK",
      options: optionsRaw
    }
  };

  const result = await apiFetch<{
    success: boolean;
    user?: LoginResult extends { kind: "authenticated" } ? LoginResult["user"] : never;
    requiresOnboarding?: boolean;
  }>("/api/auth/mfa/fido2/verify", {
    method: "POST",
    body: verifyPayload
  });

  if (!result.user) {
    throw new Error("FIDO2 verification succeeded but session payload is missing.");
  }

  return {
    kind: "authenticated",
    user: result.user,
    requiresOnboarding: result.requiresOnboarding ?? false
  };
};
