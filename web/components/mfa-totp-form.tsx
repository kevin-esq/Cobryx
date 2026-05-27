"use client";

import { useState, type FormEvent } from "react";

import { useTranslation } from "@/components/i18n-provider";
import { verifyMfaTotp, type LoginResult } from "@/lib/auth/client";

type Props = {
  mfaToken: string;
  onSuccess: (result: LoginResult) => void;
  onError: (error: unknown) => void;
};

export function MfaTotpForm({ mfaToken, onSuccess, onError }: Props) {
  const { dictionary } = useTranslation();
  const copy = dictionary.auth.mfa;
  const [code, setCode] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSubmitting(true);
    try {
      const result = await verifyMfaTotp(mfaToken, code.trim());
      onSuccess(result);
    } catch (error) {
      onError(error);
    } finally {
      setSubmitting(false);
    }
  };

  const inputClass =
    "block w-full rounded-lg border border-divider-300 bg-white px-3 py-2 text-center text-lg tracking-widest text-ink-900 shadow-sm outline-none transition focus:border-blue-500 focus:ring-2 focus:ring-blue-200 disabled:bg-white-100 dark:border-divider-600 dark:bg-ink-900 dark:text-white dark:focus:border-blue-400 dark:focus:ring-blue-500/40 dark:disabled:bg-ink-800";

  return (
    <form className="space-y-4" onSubmit={onSubmit}>
      <label className="block space-y-1.5">
        <span className="text-sm font-medium text-ink-700 dark:text-gray-200">
          {copy.totpLabel}
        </span>
        <input
          type="text"
          inputMode="numeric"
          autoComplete="one-time-code"
          pattern="[0-9]{6}"
          maxLength={6}
          required
          value={code}
          onChange={(event) => setCode(event.target.value.replace(/\D/g, ""))}
          className={inputClass}
          placeholder={copy.totpPlaceholder}
          disabled={submitting}
        />
      </label>
      <button
        type="submit"
        disabled={submitting || code.length < 6}
        className="inline-flex w-full items-center justify-center rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-blue-500 dark:text-ink-900"
      >
        {submitting ? copy.totpSubmitting : copy.totpSubmit}
      </button>
    </form>
  );
}
