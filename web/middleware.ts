import { NextRequest, NextResponse } from "next/server";

import {
  COOKIE_PATH,
  COOKIE_SAME_SITE,
  REFRESH_COOKIE,
  SESSION_COOKIE
} from "@/lib/auth/constants";

const isProduction = process.env.NODE_ENV === "production";

const parseSession = (raw: string | undefined): { expiresAt: string } | null => {
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as { expiresAt?: string };
    if (!parsed.expiresAt) return null;
    return { expiresAt: parsed.expiresAt };
  } catch {
    return null;
  }
};

export async function middleware(request: NextRequest) {
  const sessionRaw = request.cookies.get(SESSION_COOKIE)?.value;
  const session = parseSession(sessionRaw);
  const refresh = request.cookies.get(REFRESH_COOKIE)?.value;

  const loginUrl = new URL("/login", request.url);
  loginUrl.searchParams.set("returnTo", request.nextUrl.pathname);

  if (!session && !refresh) {
    return NextResponse.redirect(loginUrl);
  }

  if (session) {
    const expires = new Date(session.expiresAt).getTime();
    if (!Number.isNaN(expires) && expires > Date.now() + 30_000) {
      return NextResponse.next();
    }
  }

  if (refresh) {
    const refreshUrl = new URL("/api/auth/refresh", request.url);
    try {
      const refreshResponse = await fetch(refreshUrl, {
        method: "POST",
        headers: {
          Cookie: `${REFRESH_COOKIE}=${refresh}`,
          Accept: "application/json"
        }
      });

      if (refreshResponse.ok) {
        const next = NextResponse.next();
        for (const cookie of refreshResponse.headers.getSetCookie?.() ?? []) {
          const [pair] = cookie.split(";");
          if (!pair) continue;
          const eq = pair.indexOf("=");
          if (eq <= 0) continue;
          const name = pair.slice(0, eq).trim();
          const value = pair.slice(eq + 1);
          next.cookies.set(name, value, {
            httpOnly: true,
            secure: isProduction,
            sameSite: COOKIE_SAME_SITE,
            path: COOKIE_PATH
          });
        }
        return next;
      }
    } catch {
      // fall through to login redirect
    }
  }

  return NextResponse.redirect(loginUrl);
}

export const config = {
  matcher: ["/dashboard/:path*"]
};
