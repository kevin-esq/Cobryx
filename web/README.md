# Cobryx Web

Frontend dashboard for the Cobryx tenant-authenticated experience. Built with Next.js 15 (App Router), React 19, TypeScript and Tailwind CSS.

## Layout

This workspace lives at the monorepo root, next to the .NET projects (`Cobryx.Api/`, `Cobryx.Application/`, ...) and the Python service (`ml-service/`). It is intentionally outside the .NET solution (`Cobryx.slnx`).

```text
Cobryx/
├── Cobryx.Api/            # ASP.NET Core Web API
├── Cobryx.Application/    # MediatR handlers + business logic
├── ml-service/            # Python scoring service
└── web/                   # ← you are here
    ├── app/               # Next.js App Router pages
    ├── components/        # Reusable React components
    ├── lib/               # Client SDK, auth helpers, env
    └── public/            # Static assets
```

## Requirements

- Node.js 20+.
- A running Cobryx API. By default this app talks to `http://localhost:5142`.

## Getting started

```bash
cd web
cp .env.example .env.local
npm install        # or pnpm install / yarn install
npm run dev
```

Open <http://localhost:3000>.

The API must have `http://localhost:3000` in its CORS allow-list. The shipped `appsettings.json` already includes it via `App.AllowedOrigins`.

## Scripts

| Script             | Description                              |
| ------------------ | ---------------------------------------- |
| `npm run dev`      | Next.js dev server on port 3000.         |
| `npm run build`    | Production build.                        |
| `npm run start`    | Run the production build on port 3000.   |
| `npm run lint`     | ESLint with Next.js + TypeScript rules.  |
| `npm run typecheck`| TypeScript no-emit check.                |

## What is in this scaffold

- Landing page redirecting to `/login`.
- Login form (no real auth wiring yet, just the shell).
- Dashboard placeholder protected by a client-side token check.
- Locale switcher (Spanish default, English supported) backed by a cookie.
- `lib/api-client.ts`: minimal typed `fetch` wrapper using `NEXT_PUBLIC_API_URL`. Sends `Accept-Language` from the active locale.
- `lib/auth.ts`: client-side token storage (placeholder, to be replaced by httpOnly cookies via Route Handlers).
- `lib/env.ts`: type-safe access to public env vars.
- `lib/i18n/`: i18n stack ready for back-end error codes (see below).

## Theming (light + dark)

The app supports light, dark and system-driven themes.

- Strategy: Tailwind `darkMode: "class"`. The `dark` class lives on `<html>`.
- Persistence: cookie `cobryx.theme` (`"light" | "dark" | "system"`, default `"system"`).
- SSR snapshot: `app/layout.tsx` reads the cookie via `getServerThemePreference()` and applies `dark` when the user opted in explicitly.
- Anti-FOUC: an inline script (`lib/theme/init-script.ts`) runs synchronously in `<head>` to apply the correct class before paint, including the `system` case where the server cannot know `prefers-color-scheme`.
- Runtime: `ThemeProvider` keeps the preference in React state, listens to OS-level changes when on `system`, and exposes `useTheme()` (`{ preference, resolved, setPreference }`). `<ThemeToggle>` provides the UI.
- Authoring rule: every interactive surface includes `dark:` variants. Use `dark:bg-ink-800`/`dark:bg-ink-900` for surfaces, `dark:text-white`/`dark:text-gray-300` for typography, `dark:border-divider-600`/`dark:border-divider-700` for borders, and `dark:bg-blue-500 dark:hover:bg-blue-400` for primary actions (per the dark-mode palette guidance).

## Design tokens

The Tailwind config exposes the official Cobryx palette:

| Token       | Use                                              |
| ----------- | ------------------------------------------------ |
| `blue-*`    | Primary brand actions, links, focus rings.       |
| `info-*`    | Notifications, informational chips.              |
| `green-*`   | Success states.                                  |
| `red-*`     | Errors and destructive actions.                  |
| `accent-*`  | Warnings, highlights, badges.                    |
| `gray-*`    | Neutral surfaces and borders.                    |
| `divider-*` | Borders/separators (alias of the gray scale).    |
| `ink-*`     | Typography (`ink-900` titles, `ink-500` body).   |
| `white-*`   | Off-white surfaces (`bg-white` still works).     |

## Internationalisation (i18n)

The web app ships with a dependency-free i18n layer designed to consume the back-end error code contract.

- Locales: `es` (default), `en`. Cookie `cobryx.locale` persists the user choice and is read on the server in `app/layout.tsx`.
- Server components: `import { getServerDictionary } from "@/lib/i18n/server"` and read strings directly.
- Client components: `import { useTranslation } from "@/components/i18n-provider"` and read from `dictionary` or call `t("auth.login.title")`.
- API errors: `useTranslation().translateError(error)` maps `ApiError.body.code` (or RFC 7807 `type`, or ASP.NET `extensions.code`, or `errors[].code`) to a localized string. Unknown codes fall back to an HTTP-status-based bucket (401/403/404/422/429/5xx), then to a generic fallback.
- Adding messages: edit `lib/i18n/messages/es.ts` (source of truth for the dictionary shape). `en.ts` is statically typed against that shape, so missing keys fail typecheck.
- Adding API error codes: extend the `apiErrors` map in both `es.ts` and `en.ts` with the exact uppercase code emitted by `Cobryx.Api` (e.g. `AUTH.LOGIN.INVALID_CREDENTIALS`).

Example use in a client component:

```tsx
import { useTranslation } from "@/components/i18n-provider";
import { apiFetch, ApiError } from "@/lib/api-client";

const { t, translateError } = useTranslation();

try {
  await apiFetch("/v1/auth/login", { method: "POST", body: { email, password } });
} catch (error) {
  const { code, message } = translateError(error);
  setError({ code, message });
}
```

## What is NOT here yet

- Real authentication flow (login -> JWT -> refresh -> protected routes).
- MFA (TOTP + FIDO2) UI.
- Generated TypeScript SDK from `/swagger/v1/swagger.json`.
- Customer portal (token-based public flows). Will live in a sibling app once it appears.
- Docker service in `docker-compose.yml`.

See `docs/private/roadmap.md` (local-only) for the planned frontend track F1–F5.
