import { NextRequest } from "next/server";

import { completeAuthFromBackend, fetchBackend } from "@/lib/auth/server";

export async function POST(request: NextRequest) {
  const body = await request.json().catch(() => null);
  if (!body || typeof body !== "object") {
    return Response.json(
      { success: false, errorCode: "VALIDATION_ERROR" },
      { status: 400 }
    );
  }

  const backendResponse = await fetchBackend("/api/v1/mfa/fido2/verify", {
    method: "POST",
    body: JSON.stringify(body),
    acceptLanguage: request.headers.get("accept-language")
  });

  return completeAuthFromBackend(backendResponse);
}
