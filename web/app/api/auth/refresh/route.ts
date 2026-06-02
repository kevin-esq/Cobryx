import { NextRequest } from "next/server";

import { refreshSession } from "@/lib/auth/server";

export async function POST(request: NextRequest) {
  const refreshed = await refreshSession(request.headers.get("accept-language"));
  if (!refreshed) {
    return Response.json({ success: false }, { status: 401 });
  }
  return refreshed;
}
