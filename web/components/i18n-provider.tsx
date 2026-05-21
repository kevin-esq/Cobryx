"use client";

import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  type ReactNode
} from "react";
import { useRouter } from "next/navigation";

import { setClientLocale } from "@/lib/i18n/client";
import {
  defaultLocale,
  getDictionary,
  interpolate,
  resolveKey,
  translateApiError,
  type Dictionary,
  type InterpolationVars,
  type Locale,
  type TranslatedApiError
} from "@/lib/i18n";

interface I18nContextValue {
  locale: Locale;
  dictionary: Dictionary;
  t: (key: string, vars?: InterpolationVars) => string;
  translateError: (
    error: unknown,
    vars?: InterpolationVars
  ) => TranslatedApiError;
  changeLocale: (next: Locale) => void;
}

const I18nContext = createContext<I18nContextValue | null>(null);

interface I18nProviderProps {
  locale: Locale;
  dictionary: Dictionary;
  children: ReactNode;
}

export function I18nProvider({ locale, dictionary, children }: I18nProviderProps) {
  const router = useRouter();

  const t = useCallback(
    (key: string, vars?: InterpolationVars): string => {
      const value = resolveKey(dictionary, key);
      if (value === undefined) {
        if (process.env.NODE_ENV !== "production") {
          // eslint-disable-next-line no-console
          console.warn(`[i18n] Missing key "${key}" for locale "${locale}".`);
        }
        return key;
      }
      return interpolate(value, vars);
    },
    [dictionary, locale]
  );

  const translateError = useCallback(
    (error: unknown, vars?: InterpolationVars) =>
      translateApiError(error, dictionary, vars),
    [dictionary]
  );

  const changeLocale = useCallback(
    (next: Locale) => {
      setClientLocale(next);
      router.refresh();
    },
    [router]
  );

  const value = useMemo<I18nContextValue>(
    () => ({ locale, dictionary, t, translateError, changeLocale }),
    [locale, dictionary, t, translateError, changeLocale]
  );

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>;
}

export function useTranslation(): I18nContextValue {
  const context = useContext(I18nContext);
  if (!context) {
    // Defensive fallback so client components never crash if used outside
    // the provider (e.g. in storybook or isolated tests).
    const dictionary = getDictionary(defaultLocale);
    return {
      locale: defaultLocale,
      dictionary,
      t: (key, vars) => {
        const v = resolveKey(dictionary, key);
        return v === undefined ? key : interpolate(v, vars);
      },
      translateError: (error, vars) => translateApiError(error, dictionary, vars),
      changeLocale: () => undefined
    };
  }
  return context;
}
