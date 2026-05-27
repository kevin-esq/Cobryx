export type AuthUser = {
  email: string | null;
  firstName: string | null;
  lastName: string | null;
  fullName: string | null;
  role: string | null;
};

export type SessionPayload = {
  accessToken: string;
  expiresAt: string;
  user: AuthUser;
};

export type AuthResponseData = {
  accessToken: string | null;
  firstName: string | null;
  lastName: string | null;
  fullName: string | null;
  email: string | null;
  role: string | null;
  expires: string | null;
  sessionId: string | null;
  requiresMfa: boolean;
  mfaToken: string | null;
  requiresOnboarding: boolean;
};

export type ApiEnvelope<T> = {
  success: boolean;
  outcomeCode?: string | null;
  traceId?: string | null;
  data?: T;
  errorCode?: string | null;
  numericCode?: number | null;
  errors?: Array<{ field: string; code: string }>;
};

export type SessionContract = {
  id: string;
  ipAddress: string;
  deviceFingerprint: string | null;
  deviceName: string | null;
  lastActiveAt: string;
  isCurrent: boolean;
};
