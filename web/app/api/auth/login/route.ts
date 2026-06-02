import { NextRequest } from "next/server";

import { completeAuthFromBackend, fetchBackend } from "@/lib/auth/server";

export async function POST(request: NextRequest) {
  const body = (await request.json().catch(() => null)) as {
    email?: string;
    password?: string;
    captchaToken?: string | null;
  } | null;

  if (!body?.email || !body?.password) {
    return Response.json(
      { success: false, errorCode: "VALIDATION_ERROR" },
      { status: 400 }
    );
  }

  const backendResponse = await fetchBackend("/api/v1/auth/login", {
    method: "POST",
    body: JSON.stringify({
      email: body.email,
      password: body.password,
      captchaToken: body.captchaToken ?? null
    }),
    acceptLanguage: request.headers.get("accept-language")
  });

  return completeAuthFromBackend(backendResponse);
}
