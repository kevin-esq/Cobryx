import { NextResponse } from "next/server";

import { readSession } from "@/lib/auth/server";
import { isSessionExpired } from "@/lib/auth/session-utils";

export async function GET() {
  const session = await readSession();
  if (!session || isSessionExpired(session)) {
    return NextResponse.json({ authenticated: false }, { status: 401 });
  }

  return NextResponse.json({
    authenticated: true,
    expiresAt: session.expiresAt,
    user: session.user
  });
}
