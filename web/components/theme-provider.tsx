"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode
} from "react";

import {
  applyThemeClass,
  getClientThemePreference,
  prefersDarkColorScheme,
  setClientThemePreference
} from "@/lib/theme/client";
import {
  defaultThemePreference,
  resolveTheme,
  type ResolvedTheme,
  type ThemePreference
} from "@/lib/theme/config";

interface ThemeContextValue {
  preference: ThemePreference;
  resolved: ResolvedTheme;
  setPreference: (preference: ThemePreference) => void;
}

const ThemeContext = createContext<ThemeContextValue | null>(null);

interface ThemeProviderProps {
  initialPreference: ThemePreference;
  children: ReactNode;
}

export function ThemeProvider({ initialPreference, children }: ThemeProviderProps) {
  const [preference, setPreferenceState] = useState<ThemePreference>(initialPreference);
  const [resolved, setResolved] = useState<ResolvedTheme>(() => {
    if (initialPreference !== "system") return initialPreference;
    // SSR-safe default: matches the assumption in the inline init script.
    return "light";
  });

  // Sync the cookie-based preference once on mount in case the cookie was
  // updated in another tab or the server snapshot drifted.
  useEffect(() => {
    const cookiePreference = getClientThemePreference();
    if (cookiePreference !== preference) {
      setPreferenceState(cookiePreference);
    }
    // We only want this to run once on mount.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Apply the resolved theme + listen to OS-level changes when on "system".
  useEffect(() => {
    const apply = () => {
      const next = resolveTheme(preference, prefersDarkColorScheme());
      setResolved(next);
      applyThemeClass(next);
    };

    apply();

    if (preference !== "system" || typeof window === "undefined" || !window.matchMedia) {
      return;
    }

    const mq = window.matchMedia("(prefers-color-scheme: dark)");
    const onChange = () => apply();
    mq.addEventListener("change", onChange);
    return () => mq.removeEventListener("change", onChange);
  }, [preference]);

  const setPreference = useCallback((next: ThemePreference) => {
    setClientThemePreference(next);
    setPreferenceState(next);
  }, []);

  const value = useMemo<ThemeContextValue>(
    () => ({ preference, resolved, setPreference }),
    [preference, resolved, setPreference]
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme(): ThemeContextValue {
  const context = useContext(ThemeContext);
  if (!context) {
    return {
      preference: defaultThemePreference,
      resolved: "light",
      setPreference: () => undefined
    };
  }
  return context;
}
