export const themePreferences = ["light", "dark", "system"] as const;
export type ThemePreference = (typeof themePreferences)[number];

export type ResolvedTheme = "light" | "dark";

export const defaultThemePreference: ThemePreference = "system";

export const THEME_COOKIE = "cobryx.theme";

export function isThemePreference(value: unknown): value is ThemePreference {
  return (
    typeof value === "string" &&
    (themePreferences as readonly string[]).includes(value)
  );
}

export function resolveTheme(
  preference: ThemePreference,
  prefersDark: boolean
): ResolvedTheme {
  if (preference === "system") return prefersDark ? "dark" : "light";
  return preference;
}
