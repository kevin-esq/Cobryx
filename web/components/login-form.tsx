"use client";

import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";

import { useTranslation } from "@/components/i18n-provider";
import { setAccessToken } from "@/lib/auth";

type LoginState =
  | { kind: "idle" }
  | { kind: "submitting" }
  | { kind: "error"; message: string; code: string | null };

export function LoginForm() {
  const router = useRouter();
  const { dictionary } = useTranslation();
  const copy = dictionary.auth.login;

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [state, setState] = useState<LoginState>({ kind: "idle" });

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setState({ kind: "submitting" });

    // Placeholder: real /v1/auth/login wiring lands in roadmap item F1.
    // For now we accept any non-empty credentials and store a sentinel token.
    if (!email.trim() || !password) {
      setState({ kind: "error", message: copy.empty, code: null });
      return;
    }

    setAccessToken(`scaffold-${Date.now().toString(36)}`);
    router.push("/dashboard");
  };

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
