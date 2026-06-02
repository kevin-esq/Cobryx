# Auth API Contract (F1)

Authentication and session management for tenant users. Base path: `/api/v1/auth`.
MFA endpoints: `/api/v1/mfa`. Sessions: `/api/v1/sessions`.

## Login

`POST /api/v1/auth/login` — `[AllowAnonymous]`

**Request**

```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "captchaToken": null
}
```

**Success outcomes**

| Outcome | When |
| --- | --- |
| `AUTH.LOGIN.COMPLETED` | Credentials valid; access token in `data.accessToken`; refresh token in `Set-Cookie: refreshToken` |
| `AUTH.LOGIN.MFA_REQUIRED` | MFA enabled; `data.requiresMfa=true`, `data.mfaToken` set, no access token |

**Error outcomes**

| HTTP | `errorCode` / outcome | When |
| --- | --- | --- |
| 401 | `DOMAIN.AUTH.INVALID_CREDENTIALS` | Bad password, unverified email, locked account |

## Token refresh

`POST /api/v1/auth/token/refresh` — requires `refreshToken` HttpOnly cookie.

| Outcome | When |
| --- | --- |
| `AUTH.TOKEN.ROTATED` | New access token + rotated refresh cookie |

## Logout

| Endpoint | Outcome |
| --- | --- |
| `POST /api/v1/auth/logout` | `AUTH.LOGOUT.COMPLETED` |
| `POST /api/v1/auth/sessions/revoke-all` | `AUTH.LOGOUT_ALL.COMPLETED` |

## MFA (login flow)

| Endpoint | Outcome |
| --- | --- |
| `POST /api/v1/mfa/totp/verify` | `AUTH.MFA.VERIFIED` — body: `{ "code", "persistenceToken" }` (same value as `mfaToken` from login) |
| `POST /api/v1/mfa/fido2/challenge` | `AUTH.MFA.INITIATED` — body: `{ "persistenceToken" }` |
| `POST /api/v1/mfa/fido2/verify` | `AUTH.MFA.VERIFIED` |

On MFA success the API sets `Set-Cookie: refreshToken` and returns `AuthResponseContract` in `data`.

## Sessions

| Endpoint | Outcome |
| --- | --- |
| `GET /api/v1/sessions` | `AUTH.SESSION.SEARCH.COMPLETED` |
| `DELETE /api/v1/sessions/{id}` | `AUTH.SESSION.REVOKED` (204) |

## Web BFF (Next.js, not public API)

The `web/` app exposes same-origin route handlers so the browser never stores tokens in
`localStorage`. Cookies: `cobryx_session` (access token + user metadata), `cobryx_refresh`
(backend refresh token).

| BFF route | Proxies to |
| --- | --- |
| `POST /api/auth/login` | `POST /api/v1/auth/login` |
| `POST /api/auth/refresh` | `POST /api/v1/auth/token/refresh` |
| `POST /api/auth/logout` | `POST /api/v1/auth/logout` |
| `GET /api/auth/session` | (reads `cobryx_session` cookie locally) |
| `POST /api/auth/mfa/totp` | `POST /api/v1/mfa/totp/verify` |
| `POST /api/auth/mfa/fido2/challenge` | `POST /api/v1/mfa/fido2/challenge` |
| `POST /api/auth/mfa/fido2/verify` | `POST /api/v1/mfa/fido2/verify` |
| `POST /api/auth/sessions/revoke-all` | `POST /api/v1/auth/sessions/revoke-all` |
| `GET /api/sessions` | `GET /api/v1/sessions` |
| `DELETE /api/sessions/{id}` | `DELETE /api/v1/sessions/{id}` |

Configure `INTERNAL_API_URL` (or `API_URL`) in `web/.env.local` to point at Cobryx.Api.
