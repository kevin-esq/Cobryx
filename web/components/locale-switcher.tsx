"use client";

import type { ChangeEvent } from "react";

import { locales, type Locale } from "@/lib/i18n/config";
import { useTranslation } from "@/components/i18n-provider";

export function LocaleSwitcher() {
  const { locale, dictionary, changeLocale } = useTranslation();

  const onChange = (event: ChangeEvent<HTMLSelectElement>) => {
    changeLocale(event.target.value as Locale);
  };

  return (
    <label className="flex items-center gap-2 text-xs font-medium text-ink-500 dark:text-gray-300">
      <span className="sr-only">{dictionary.common.languageLabel}</span>
      <select
        aria-label={dictionary.common.languageLabel}
        value={locale}
        onChange={onChange}
        className="rounded-md border border-divider-200 bg-white px-2 py-1 text-xs text-ink-700 shadow-sm outline-none transition focus:border-blue-500 focus:ring-2 focus:ring-blue-200 dark:border-divider-600 dark:bg-ink-800 dark:text-gray-100 dark:focus:border-blue-400 dark:focus:ring-blue-500/40"
      >
        {locales.map((code) => (
          <option key={code} value={code}>
            {dictionary.common.languageOptions[code]}
          </option>
        ))}
      </select>
    </label>
  );
}
