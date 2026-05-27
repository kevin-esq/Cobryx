import { NextRequest, NextResponse } from "next/server";

import {
  clearAuthCookies,
  jsonFromBackend,
  mergeRefreshCookies,
  proxyAuthenticated
} from "@/lib/auth/server";

export async function POST(request: NextRequest) {
  const { response, refreshed } = await proxyAuthenticated(
    "/api/v1/auth/sessions/revoke-all",
    {
      method: "POST",
      acceptLanguage: request.headers.get("accept-language")
    }
  );

  const { status, body } = await jsonFromBackend<unknown>(response);
  const res = NextResponse.json(body ?? { success: response.ok }, { status });
  if (response.ok) {
    clearAuthCookies(res);
  }
  return mergeRefreshCookies(res, refreshed);
}
