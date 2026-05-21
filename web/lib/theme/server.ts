// Server-only. Imports `next/headers`, which throws if used in a client component.
import { cookies } from "next/headers";

import {
  defaultThemePreference,
  isThemePreference,
  THEME_COOKIE,
  type ThemePreference
} from "./config";

export async function getServerThemePreference(): Promise<ThemePreference> {
  const store = await cookies();
  const value = store.get(THEME_COOKIE)?.value;
  return isThemePreference(value) ? value : defaultThemePreference;
}
