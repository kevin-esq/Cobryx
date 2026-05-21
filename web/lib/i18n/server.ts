// This module is intentionally server-only. It relies on `next/headers`,
// which throws when imported from a client component, providing a runtime
// guard equivalent to the `server-only` package without an extra dependency.
import { cookies } from "next/headers";

import { defaultLocale, isLocale, LOCALE_COOKIE, type Locale } from "./config";
import { getDictionary, type Dictionary } from "./dictionaries";

export async function getServerLocale(): Promise<Locale> {
  const store = await cookies();
  const value = store.get(LOCALE_COOKIE)?.value;
  return isLocale(value) ? value : defaultLocale;
}

export async function getServerDictionary(): Promise<{
  locale: Locale;
  dictionary: Dictionary;
}> {
  const locale = await getServerLocale();
  return { locale, dictionary: getDictionary(locale) };
}
