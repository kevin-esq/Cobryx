import type { Metadata } from "next";

import { LoginForm } from "@/components/login-form";
import { LocaleSwitcher } from "@/components/locale-switcher";
import { ThemeToggle } from "@/components/theme-toggle";
import { getServerDictionary } from "@/lib/i18n/server";

export async function generateMetadata(): Promise<Metadata> {
  const { dictionary } = await getServerDictionary();
  return { title: dictionary.auth.login.title };
}

export default async function LoginPage() {
  const { dictionary } = await getServerDictionary();
  const copy = dictionary.auth.login;

  return (
    <main className="flex min-h-screen items-center justify-center bg-gradient-to-br from-blue-50 via-white to-blue-100 px-4 py-12 dark:from-ink-900 dark:via-ink-800 dark:to-ink-900">
      <div className="w-full max-w-md rounded-2xl border border-divider-200 bg-white p-8 shadow-sm dark:border-divider-700 dark:bg-ink-800 dark:shadow-none">
        <div className="mb-6 flex items-start justify-between gap-4">
          <header className="space-y-1">
            <p className="text-xs font-semibold uppercase tracking-widest text-blue-600 dark:text-blue-400">
              {copy.eyebrow}
            </p>
            <h1 className="text-2xl font-semibold text-ink-900 dark:text-white">
              {copy.title}
            </h1>
            <p className="text-sm text-ink-500 dark:text-gray-300">
              {copy.description}
            </p>
          </header>
          <div className="flex flex-col items-end gap-2">
            <LocaleSwitcher />
            <ThemeToggle />
          </div>
        </div>

        <LoginForm />

        <p className="mt-6 text-center text-xs text-ink-400 dark:text-gray-400">
          {copy.scaffoldNotice}
        </p>
      </div>
    </main>
  );
}
