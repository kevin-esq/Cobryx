import { NextRequest, NextResponse } from "next/server";

import { fetchBackend, jsonFromBackend } from "@/lib/auth/server";

export async function POST(request: NextRequest) {
  const body = (await request.json().catch(() => null)) as {
    persistenceToken?: string;
    mfaToken?: string;
  } | null;

  const persistenceToken = body?.persistenceToken ?? body?.mfaToken;
  if (!persistenceToken) {
    return NextResponse.json(
      { success: false, errorCode: "VALIDATION_ERROR" },
      { status: 400 }
    );
  }

  const backendResponse = await fetchBackend("/api/v1/mfa/fido2/challenge", {
    method: "POST",
    body: JSON.stringify({ persistenceToken }),
    acceptLanguage: request.headers.get("accept-language")
  });

  const { status, body: envelope } = await jsonFromBackend<unknown>(backendResponse);
  return NextResponse.json(envelope ?? { success: false }, { status });
}
