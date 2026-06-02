import type { Metadata } from "next";

import { SessionsList } from "@/components/sessions-list";
import { LocaleSwitcher } from "@/components/locale-switcher";
import { ThemeToggle } from "@/components/theme-toggle";
import { getServerDictionary } from "@/lib/i18n/server";

export async function generateMetadata(): Promise<Metadata> {
  const { dictionary } = await getServerDictionary();
  return { title: dictionary.auth.sessions.title };
}

export default async function SessionsPage() {
  const { dictionary } = await getServerDictionary();
  const copy = dictionary.auth.sessions;

  return (
    <main className="min-h-screen bg-gray-50 dark:bg-ink-900">
      <header className="border-b border-divider-200 bg-white dark:border-divider-700 dark:bg-ink-800">
        <div className="mx-auto flex max-w-3xl items-center justify-between px-6 py-4">
          <div>
            <h1 className="text-lg font-semibold text-ink-900 dark:text-white">
              {copy.title}
            </h1>
            <p className="text-sm text-ink-500 dark:text-gray-300">
              {copy.description}
            </p>
          </div>
          <div className="flex items-center gap-2">
            <LocaleSwitcher />
            <ThemeToggle />
          </div>
        </div>
      </header>
      <div className="mx-auto max-w-3xl px-6 py-10">
        <SessionsList />
      </div>
    </main>
  );
}
