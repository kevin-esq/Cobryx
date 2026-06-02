import { cookies } from "next/headers";
import { NextResponse } from "next/server";

import { serverEnv } from "@/lib/env";

import {
  BACKEND_REFRESH_COOKIE,
  COOKIE_PATH,
  COOKIE_SAME_SITE,
  REFRESH_COOKIE,
  SESSION_COOKIE
} from "./constants";
import { collectSetCookieHeader, parseRefreshTokenFromSetCookie } from "./cookies";
import { isSessionExpired } from "./session-utils";
import type {
  ApiEnvelope,
  AuthResponseData,
  SessionPayload,
  SessionContract
} from "./types";

const isProduction = process.env.NODE_ENV === "production";

export const getInternalApiBase = (): string =>
  serverEnv.internalApiUrl.replace(/\/+$/, "");

export const buildApiUrl = (path: string): string => {
  const suffix = path.startsWith("/") ? path : `/${path}`;
  return `${getInternalApiBase()}${suffix}`;
};

export { collectSetCookieHeader, parseRefreshTokenFromSetCookie } from "./cookies";

export const sessionFromAuthData = (data: AuthResponseData): SessionPayload | null => {
  if (!data.accessToken || !data.expires) return null;

  return {
    accessToken: data.accessToken,
    expiresAt: data.expires,
    user: {
      email: data.email,
      firstName: data.firstName,
      lastName: data.lastName,
      fullName: data.fullName,
      role: data.role
    }
  };
};

export const applyAuthCookies = (
  response: NextResponse,
  session: SessionPayload,
  refreshToken?: { value: string; expires?: Date }
): void => {
  const expiresAt = new Date(session.expiresAt);
  const maxAge = Math.max(
    0,
    Math.floor((expiresAt.getTime() - Date.now()) / 1000)
  );

  response.cookies.set(SESSION_COOKIE, JSON.stringify(session), {
    httpOnly: true,
    secure: isProduction,
    sameSite: COOKIE_SAME_SITE,
    path: COOKIE_PATH,
    expires: expiresAt,
    maxAge
  });

  if (refreshToken) {
    response.cookies.set(REFRESH_COOKIE, refreshToken.value, {
      httpOnly: true,
      secure: isProduction,
      sameSite: COOKIE_SAME_SITE,
      path: COOKIE_PATH,
      expires: refreshToken.expires,
      maxAge: refreshToken.expires
        ? Math.max(
            0,
            Math.floor((refreshToken.expires.getTime() - Date.now()) / 1000)
          )
        : 60 * 60 * 24 * 30
    });
  }
};

export const clearAuthCookies = (response: NextResponse): void => {
  response.cookies.set(SESSION_COOKIE, "", {
    httpOnly: true,
    secure: isProduction,
    sameSite: COOKIE_SAME_SITE,
    path: COOKIE_PATH,
    maxAge: 0
  });
  response.cookies.set(REFRESH_COOKIE, "", {
    httpOnly: true,
    secure: isProduction,
    sameSite: COOKIE_SAME_SITE,
    path: COOKIE_PATH,
    maxAge: 0
  });
};

export const readSession = async (): Promise<SessionPayload | null> => {
  const jar = await cookies();
  const raw = jar.get(SESSION_COOKIE)?.value;
  if (!raw) return null;

  try {
    const parsed = JSON.parse(raw) as SessionPayload;
    if (!parsed.accessToken || !parsed.expiresAt) return null;
    return parsed;
  } catch {
    return null;
  }
};

export const readRefreshToken = async (): Promise<string | null> => {
  const jar = await cookies();
  return jar.get(REFRESH_COOKIE)?.value ?? null;
};

export { isSessionExpired } from "./session-utils";

type BackendFetchInit = RequestInit & {
  accessToken?: string | null;
  refreshToken?: string | null;
  acceptLanguage?: string | null;
};

export const fetchBackend = async (
  path: string,
  init: BackendFetchInit = {}
): Promise<Response> => {
  const { accessToken, refreshToken, acceptLanguage, headers, ...rest } = init;

  const finalHeaders = new Headers(headers);
  finalHeaders.set("Accept", "application/json");
  if (acceptLanguage) {
    finalHeaders.set("Accept-Language", acceptLanguage);
  }
  if (accessToken) {
    finalHeaders.set("Authorization", `Bearer ${accessToken}`);
  }

  const cookieParts: string[] = [];
  if (refreshToken) {
    cookieParts.push(`${BACKEND_REFRESH_COOKIE}=${refreshToken}`);
  }
  if (cookieParts.length > 0) {
    finalHeaders.set("Cookie", cookieParts.join("; "));
  }

  if (
    rest.body !== undefined &&
    rest.body !== null &&
    !(rest.body instanceof FormData) &&
    !finalHeaders.has("Content-Type")
  ) {
    finalHeaders.set("Content-Type", "application/json");
  }

  return fetch(buildApiUrl(path), {
    ...rest,
    headers: finalHeaders,
    cache: "no-store"
  });
};

export const parseJson = async <T>(response: Response): Promise<T | null> => {
  const contentType = response.headers.get("content-type") ?? "";
  if (!contentType.includes("application/json")) return null;
  return (await response.json().catch(() => null)) as T | null;
};

export const jsonFromBackend = async <T>(
  response: Response
): Promise<{ status: number; body: ApiEnvelope<T> | null; setCookie: string | null }> => {
  const body = await parseJson<ApiEnvelope<T>>(response);
  return {
    status: response.status,
    body,
    setCookie: collectSetCookieHeader(response)
  };
};

export const completeAuthFromBackend = async (
  backendResponse: Response
): Promise<NextResponse> => {
  const { status, body, setCookie } = await jsonFromBackend<AuthResponseData>(
    backendResponse
  );

  if (!body?.success || !body.data) {
    return NextResponse.json(body ?? { success: false }, { status });
  }

  const data = body.data;
  if (data.requiresMfa || !data.accessToken) {
    return NextResponse.json(
      {
        success: true,
        outcomeCode: body.outcomeCode,
        requiresMfa: true,
        mfaToken: data.mfaToken,
        requiresOnboarding: data.requiresOnboarding
      },
      { status: 200 }
    );
  }

  const session = sessionFromAuthData(data);
  if (!session) {
    return NextResponse.json(
      { success: false, errorCode: "AUTH.SESSION.INVALID" },
      { status: 500 }
    );
  }

  const refresh = parseRefreshTokenFromSetCookie(setCookie);
  const res = NextResponse.json({
    success: true,
    outcomeCode: body.outcomeCode,
    requiresMfa: false,
    requiresOnboarding: data.requiresOnboarding,
    user: session.user
  });
  applyAuthCookies(res, session, refresh ?? undefined);
  return res;
};

export const refreshSession = async (
  acceptLanguage?: string | null
): Promise<NextResponse | null> => {
  const refreshToken = await readRefreshToken();
  if (!refreshToken) return null;

  const backendResponse = await fetchBackend("/api/v1/auth/token/refresh", {
    method: "POST",
    refreshToken,
    acceptLanguage
  });

  if (!backendResponse.ok) return null;

  return completeAuthFromBackend(backendResponse);
};

export const proxyAuthenticated = async (
  path: string,
  init: BackendFetchInit
): Promise<{ response: Response; refreshed: NextResponse | null }> => {
  let session = await readSession();
  let refreshed: NextResponse | null = null;

  if (!session || isSessionExpired(session)) {
    refreshed = await refreshSession(init.acceptLanguage);
    if (refreshed) {
      session = await readSession();
    }
  }

  if (!session) {
    return {
      response: new Response(null, { status: 401 }),
      refreshed
    };
  }

  const response = await fetchBackend(path, {
    ...init,
    accessToken: session.accessToken,
    refreshToken: await readRefreshToken()
  });

  if (response.status === 401) {
    const retryRefresh = await refreshSession(init.acceptLanguage);
    if (retryRefresh) {
      const retrySession = await readSession();
      if (retrySession) {
        const retryResponse = await fetchBackend(path, {
          ...init,
          accessToken: retrySession.accessToken,
          refreshToken: await readRefreshToken()
        });
        return { response: retryResponse, refreshed: retryRefresh };
      }
    }
  }

  return { response, refreshed };
};

export const mergeRefreshCookies = (
  clientResponse: NextResponse,
  refreshed: NextResponse | null
): NextResponse => {
  if (!refreshed) return clientResponse;

  for (const cookie of refreshed.cookies.getAll()) {
    clientResponse.cookies.set(cookie);
  }
  return clientResponse;
};

export type { SessionContract };
