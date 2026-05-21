import type { Metadata } from "next";
import type { ReactNode } from "react";

import { I18nProvider } from "@/components/i18n-provider";
import { ThemeProvider } from "@/components/theme-provider";
import { getServerDictionary } from "@/lib/i18n/server";
import { getServerThemePreference } from "@/lib/theme/server";
import { themeInitScript } from "@/lib/theme/init-script";

import "./globals.css";

export const metadata: Metadata = {
  title: {
    default: "Cobryx",
    template: "%s · Cobryx"
  },
  description: "Cobryx tenant dashboard.",
  robots: { index: false, follow: false }
};

export default async function RootLayout({ children }: { children: ReactNode }) {
  const [{ locale, dictionary }, themePreference] = await Promise.all([
    getServerDictionary(),
    getServerThemePreference()
  ]);

  // The server cannot know `prefers-color-scheme`. We render the document
  // untouched (or with `dark` when the user explicitly opted in) and let the
  // inline init script add/remove `.dark` before paint. `suppressHydrationWarning`
  // silences the resulting className mismatch when the script flips it.
  const initialDarkClass = themePreference === "dark" ? "dark" : "";

  return (
    <html lang={locale} className={initialDarkClass} suppressHydrationWarning>
      <head>
        <script dangerouslySetInnerHTML={{ __html: themeInitScript }} />
      </head>
      <body className="min-h-screen font-sans">
        <ThemeProvider initialPreference={themePreference}>
          <I18nProvider locale={locale} dictionary={dictionary}>
            {children}
          </I18nProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
