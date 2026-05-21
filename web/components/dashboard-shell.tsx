"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

import { LocaleSwitcher } from "@/components/locale-switcher";
import { ThemeToggle } from "@/components/theme-toggle";
import { useTranslation } from "@/components/i18n-provider";
import { clearAccessToken, getAccessToken } from "@/lib/auth";

type ModuleKey = "customers" | "credits" | "payments" | "invoicing" | "settings";
const moduleKeys: readonly ModuleKey[] = [
  "customers",
  "credits",
  "payments",
  "invoicing",
  "settings"
];

export function DashboardShell() {
  const router = useRouter();
  const { dictionary } = useTranslation();
  const copy = dictionary.dashboard;

  const [ready, setReady] = useState(false);

  useEffect(() => {
    if (!getAccessToken()) {
      router.replace("/login");
      return;
    }
    setReady(true);
  }, [router]);

  const onLogout = () => {
    clearAccessToken();
    router.replace("/login");
  };

  if (!ready) {
    return null;
  }

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-ink-900">
      <header className="border-b border-divider-200 bg-white dark:border-divider-700 dark:bg-ink-800">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <div className="space-y-0.5">
            <p className="text-xs font-semibold uppercase tracking-widest text-blue-600 dark:text-blue-400">
              {copy.workspaceLabel}
            </p>
            <h1 className="text-lg font-semibold text-ink-900 dark:text-white">
              {copy.title}
            </h1>
          </div>
          <div className="flex items-center gap-3">
            <LocaleSwitcher />
            <ThemeToggle />
            <button
              type="button"
              onClick={onLogout}
              className="rounded-md border border-divider-200 px-3 py-1.5 text-sm font-medium text-ink-700 transition hover:border-divider-300 hover:bg-gray-100 dark:border-divider-600 dark:text-gray-200 dark:hover:border-divider-500 dark:hover:bg-ink-700"
            >
              {copy.signOut}
            </button>
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-6xl px-6 py-10">
        <section className="mb-8 rounded-2xl border border-blue-100 bg-blue-50/60 p-6 dark:border-blue-500/30 dark:bg-blue-500/10">
          <h2 className="text-lg font-semibold text-blue-900 dark:text-blue-200">
            {copy.scaffoldHeadline}
          </h2>
          <p className="mt-1 text-sm text-blue-800/80 dark:text-blue-100/80">
            {copy.scaffoldDescription}
          </p>
        </section>

        <ul className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {moduleKeys.map((key) => {
            const item = copy.modules[key];
            return (
              <li
                key={key}
                className="rounded-xl border border-divider-200 bg-white p-5 shadow-sm dark:border-divider-700 dark:bg-ink-800 dark:shadow-none"
              >
                <h3 className="text-sm font-semibold text-ink-900 dark:text-white">
                  {item.title}
                </h3>
                <p className="mt-1 text-sm text-ink-500 dark:text-gray-300">
                  {item.description}
                </p>
                <p className="mt-4 text-xs font-medium text-ink-400 dark:text-gray-500">
                  {copy.comingSoon}
                </p>
              </li>
            );
          })}
        </ul>
      </main>
    </div>
  );
}
