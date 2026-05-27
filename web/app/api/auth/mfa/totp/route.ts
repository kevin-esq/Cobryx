import { NextRequest } from "next/server";

import { completeAuthFromBackend, fetchBackend } from "@/lib/auth/server";

export async function POST(request: NextRequest) {
  const body = (await request.json().catch(() => null)) as {
    code?: string;
    persistenceToken?: string;
    mfaToken?: string;
  } | null;

  const persistenceToken = body?.persistenceToken ?? body?.mfaToken;
  if (!body?.code || !persistenceToken) {
    return Response.json(
      { success: false, errorCode: "VALIDATION_ERROR" },
      { status: 400 }
    );
  }

  const backendResponse = await fetchBackend("/api/v1/mfa/totp/verify", {
    method: "POST",
    body: JSON.stringify({
      code: body.code,
      persistenceToken
    }),
    acceptLanguage: request.headers.get("accept-language")
  });

  return completeAuthFromBackend(backendResponse);
}
