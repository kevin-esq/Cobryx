"use client";

import { useState } from "react";

import { useTranslation } from "@/components/i18n-provider";
import type { LoginResult } from "@/lib/auth/client";
import { verifyMfaFido2 } from "@/lib/auth/fido2";

type Props = {
  mfaToken: string;
  onSuccess: (result: LoginResult) => void;
  onError: (error: unknown) => void;
};

export function MfaFido2Button({ mfaToken, onSuccess, onError }: Props) {
  const { dictionary } = useTranslation();
  const copy = dictionary.auth.mfa;
  const [submitting, setSubmitting] = useState(false);

  const onClick = async () => {
    if (!window.PublicKeyCredential) {
      onError(new Error("WebAuthn is not supported in this browser."));
      return;
    }

    setSubmitting(true);
    try {
      const result = await verifyMfaFido2(mfaToken);
      onSuccess(result);
    } catch (error) {
      onError(error);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <button
      type="button"
      onClick={onClick}
      disabled={submitting}
      className="inline-flex w-full items-center justify-center rounded-lg border border-divider-300 bg-white px-4 py-2.5 text-sm font-semibold text-ink-800 shadow-sm transition hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-60 dark:border-divider-600 dark:bg-ink-900 dark:text-gray-100 dark:hover:bg-ink-700"
    >
      {submitting ? copy.fido2Submitting : copy.fido2Button}
    </button>
  );
}
