import { NextRequest, NextResponse } from "next/server";

import { mergeRefreshCookies, proxyAuthenticated } from "@/lib/auth/server";

type RouteContext = { params: Promise<{ id: string }> };

export async function DELETE(request: NextRequest, context: RouteContext) {
  const { id } = await context.params;

  const { response, refreshed } = await proxyAuthenticated(
    `/api/v1/sessions/${id}`,
    {
      method: "DELETE",
      acceptLanguage: request.headers.get("accept-language")
    }
  );

  if (response.status === 204) {
    const res = new NextResponse(null, { status: 204 });
    return mergeRefreshCookies(res, refreshed);
  }

  const text = await response.text().catch(() => "");
  const res = new NextResponse(text || null, { status: response.status });
  return mergeRefreshCookies(res, refreshed);
}
