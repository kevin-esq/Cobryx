"use client";

import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";

import { useTranslation } from "@/components/i18n-provider";
import { MfaFido2Button } from "@/components/mfa-fido2-button";
import { MfaTotpForm } from "@/components/mfa-totp-form";
import { login, type LoginResult } from "@/lib/auth/client";
import { translateApiError } from "@/lib/i18n/error-codes";

type LoginState =
  | { kind: "idle" }
  | { kind: "submitting" }
  | { kind: "mfa"; mfaToken: string }
  | { kind: "error"; message: string; code: string | null };

export function LoginForm() {
  const router = useRouter();
  const { dictionary } = useTranslation();
  const copy = dictionary.auth.login;
  const mfaCopy = dictionary.auth.mfa;

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [state, setState] = useState<LoginState>({ kind: "idle" });

  const finishAuth = (result: LoginResult) => {
    if (result.requiresOnboarding) {
      router.push("/dashboard");
      return;
    }
    router.push("/dashboard");
  };

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setState({ kind: "submitting" });

    if (!email.trim() || !password) {
      setState({ kind: "error", message: copy.empty, code: null });
      return;
    }

    try {
      const result = await login(email.trim(), password);
      if (result.kind === "mfa") {
        setState({ kind: "mfa", mfaToken: result.mfaToken });
        return;
      }
      finishAuth(result);
    } catch (error) {
      const translated = translateApiError(error, dictionary);
      setState({
        kind: "error",
        message: translated.message,
        code: translated.code
      });
    }
  };

  const onMfaSuccess = (result: LoginResult) => {
    finishAuth(result);
  };

  const onMfaError = (error: unknown) => {
    const translated = translateApiError(error, dictionary);
    setState({
      kind: "error",
      message: translated.message,
      code: translated.code
    });
  };

  if (state.kind === "mfa") {
    return (
      <div className="space-y-6">
        <div className="space-y-1">
          <h2 className="text-lg font-semibold text-ink-900 dark:text-white">
            {mfaCopy.title}
          </h2>
          <p className="text-sm text-ink-500 dark:text-gray-300">
            {mfaCopy.description}
          </p>
        </div>

        <MfaTotpForm
          mfaToken={state.mfaToken}
          onSuccess={onMfaSuccess}
          onError={onMfaError}
        />

        <div className="relative">
          <div className="absolute inset-0 flex items-center">
            <span className="w-full border-t border-divider-200 dark:border-divider-600" />
          </div>
          <div className="relative flex justify-center text-xs uppercase">
            <span className="bg-white px-2 text-ink-400 dark:bg-ink-800 dark:text-gray-400">
              {mfaCopy.orDivider}
            </span>
          </div>
        </div>

        <MfaFido2Button
          mfaToken={state.mfaToken}
          onSuccess={onMfaSuccess}
          onError={onMfaError}
        />

        <button
          type="button"
          onClick={() => setState({ kind: "idle" })}
          className="w-full text-center text-sm text-ink-500 underline-offset-2 hover:underline dark:text-gray-300"
        >
          {mfaCopy.backToLogin}
        </button>
      </div>
    );
  }

  const submitting = state.kind === "submitting";

  const inputClass =
    "block w-full rounded-lg border border-divider-300 bg-white px-3 py-2 text-sm text-ink-900 shadow-sm outline-none transition placeholder:text-ink-300 focus:border-blue-500 focus:ring-2 focus:ring-blue-200 disabled:bg-white-100 dark:border-divider-600 dark:bg-ink-900 dark:text-white dark:placeholder:text-gray-500 dark:focus:border-blue-400 dark:focus:ring-blue-500/40 dark:disabled:bg-ink-800";

  return (
    <form className="space-y-4" onSubmit={onSubmit} noValidate>
      <label className="block space-y-1.5">
        <span className="text-sm font-medium text-ink-700 dark:text-gray-200">
          {copy.emailLabel}
        </span>
        <input
          type="email"
          autoComplete="email"
          required
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          className={inputClass}
          placeholder={copy.emailPlaceholder}
          disabled={submitting}
        />
      </label>

      <label className="block space-y-1.5">
        <span className="text-sm font-medium text-ink-700 dark:text-gray-200">
          {copy.passwordLabel}
        </span>
        <input
          type="password"
          autoComplete="current-password"
          required
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          className={inputClass}
          placeholder={copy.passwordPlaceholder}
          disabled={submitting}
        />
      </label>

      {state.kind === "error" ? (
        <p
          role="alert"
          data-code={state.code ?? undefined}
          className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-700 dark:border-red-900/60 dark:bg-red-900/30 dark:text-red-300"
        >
          {state.message}
        </p>
      ) : null}

      <button
        type="submit"
        disabled={submitting}
        className="inline-flex w-full items-center justify-center rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-300 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-blue-500 dark:text-ink-900 dark:hover:bg-blue-400 dark:focus:ring-blue-500/40 dark:focus:ring-offset-ink-800"
      >
        {submitting ? copy.submitting : copy.submit}
      </button>
    </form>
  );
}
