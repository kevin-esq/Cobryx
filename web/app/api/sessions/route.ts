import { NextRequest, NextResponse } from "next/server";

import type { SessionContract } from "@/lib/auth/types";
import {
  jsonFromBackend,
  mergeRefreshCookies,
  proxyAuthenticated
} from "@/lib/auth/server";

export async function GET(request: NextRequest) {
  const { response, refreshed } = await proxyAuthenticated("/api/v1/sessions", {
    method: "GET",
    acceptLanguage: request.headers.get("accept-language")
  });

  const { status, body } = await jsonFromBackend<SessionContract[]>(response);
  const res = NextResponse.json(body ?? { success: false }, { status });
  return mergeRefreshCookies(res, refreshed);
}
