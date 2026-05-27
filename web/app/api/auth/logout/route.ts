import { NextRequest, NextResponse } from "next/server";

import {
  clearAuthCookies,
  fetchBackend,
  mergeRefreshCookies,
  proxyAuthenticated,
  readRefreshToken
} from "@/lib/auth/server";

export async function POST(request: NextRequest) {
  const refreshToken = await readRefreshToken();
  const { response, refreshed } = await proxyAuthenticated("/api/v1/auth/logout", {
    method: "POST",
    acceptLanguage: request.headers.get("accept-language")
  });

  if (response.status === 401 && refreshToken) {
    await fetchBackend("/api/v1/auth/logout", {
      method: "POST",
      refreshToken,
      acceptLanguage: request.headers.get("accept-language")
    });
  }

  const res = NextResponse.json({ success: true });
  clearAuthCookies(res);
  return mergeRefreshCookies(res, refreshed);
}
