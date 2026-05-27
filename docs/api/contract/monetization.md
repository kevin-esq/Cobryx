# Monetization Contract (F4)

This document defines the public API contract for **plan enforcement** and **subscription monetization** flows exercised by [`docs/load-tests/monetization-stress.js`](../../load-tests/monetization-stress.js).

It complements [outcomes.md](outcomes.md), [errors.md](errors.md), and [validations.md](validations.md).

---

## Base URL and versioning

| Item | Value |
| --- | --- |
| Base path | `/api/v1` |
| Default local URL | `http://localhost:5142/api/v1` (see `Cobryx.Api/Properties/launchSettings.json`) |
| k6 override | `API_URL` env var (must include `/api/v1`) |

---

## Authentication and tenant context

All endpoints in this contract (except **Get plans**) require a valid Bearer token.

| Header | Required | Description |
| --- | --- | --- |
| `Authorization` | Yes | `Bearer {access_token}` from `POST /api/v1/auth/login` |
| `X-Tenant-Id` | Yes* | Tenant GUID. When authenticated, the token claim wins; header must not contradict the token. |
| `Content-Type` | Yes (writes) | `application/json` |
| `X-Idempotency-Key` | Yes | Required for `POST /api/v1/invoices` (`[Idempotent]` action) |

\*Not required for `GET /api/v1/subscription/plans` (`[AllowAnonymous]`).

---

## Response envelope

Success responses use `ApiSuccessResponse<T>`:

```json
{
  "success": true,
  "traceId": "0HN72V0R8M5E1:00000001",
  "outcomeCode": "BILLING.INVOICE.CREATE_SUCCESS",
  "data": { }
}
```

Errors use `ApiErrorResponse` with `errorCode`, `numericCode`, and optional `metadata` (see [errors.md](errors.md)).

---

## Endpoints

### 1. Create invoice (plan limit enforcement)

**`POST /api/v1/invoices`**

Creates a commercial invoice. Before persistence, the handler calls `ISubscriptionEnforcementService.EnsureWithinInvoicesLimitAsync`. When the tenant exceeds the plan invoice quota, the API returns **403**.

| Item | Value |
| --- | --- |
| Auth | Required |
| Idempotency | `X-Idempotency-Key` required |
| Success HTTP | `201 Created` |
| Success outcome | `BILLING.INVOICE.CREATE_SUCCESS` |
| Success body `data` | `Guid` — new invoice id |
| Plan limit HTTP | `403 Forbidden` |
| Plan limit error | `DOMAIN.SUBSCRIPTION.LIMIT_REACHED` (`numericCode`: 7001) |

**Request body** (`CreateInvoiceRequest`):

```json
{
  "customerId": "4fa85f64-5717-4562-b3fc-2c963f66afa6",
  "dueDate": "2026-12-31T23:59:59Z",
  "notes": "Optional",
  "items": [
    {
      "description": "Professional services",
      "quantity": 1,
      "unitPrice": 100.0,
      "taxConfigurationId": null
    }
  ]
}
```

**Other relevant errors**

| HTTP | errorCode | When |
| --- | --- | --- |
| 401 | `AUTH.NOT_AUTHENTICATED` | Missing/invalid token |
| 404 | `CUSTOMER.NOT_FOUND` | Customer missing or wrong tenant |
| 400 | `VALIDATION.FAILED` | Invalid payload |
| 403 | `DOMAIN.TENANT.CONTEXT_MISSING` | No tenant context resolved |

**Stress-test acceptance**: Under load, **`201` or `403`** are both valid outcomes for invoice creation (403 = plan limit reached).

---

### 2. Get subscription status

**`GET /api/v1/subscription/status`**

Returns the current tenant subscription entitlements (plan name, tier, limits, trial/grace dates).

| Item | Value |
| --- | --- |
| Auth | Required |
| Success HTTP | `200 OK` |
| Success outcome | `BILLING.SUBSCRIPTION.STATUS_CHECK_SUCCESS` |
| Success body `data` | `SubscriptionStatusDto` |

**`SubscriptionStatusDto` fields**: `status`, `planName`, `tier`, `maxInvoices`, `maxUsers`, `trialEndsAtUtc`, `gracePeriodEndsAtUtc`, `hasStripeCustomer`.

| HTTP | errorCode | When |
| --- | --- | --- |
| 404 | `DOMAIN.SUBSCRIPTION.NOT_FOUND` | No subscription for tenant |

---

### 3. Create checkout session (upgrade / purchase)

**`POST /api/v1/subscription/checkout`**

Initiates a Stripe Checkout session for plan upgrade or initial purchase. This is the **HTTP replacement** for the legacy (non-exposed) `UpgradeSubscriptionCommand`.

| Item | Value |
| --- | --- |
| Auth | Required (`[AllowExpiredSubscription]`) |
| Success HTTP | `200 OK` |
| Success outcome | `BILLING.SUBSCRIPTION.CHECKOUT_CREATE_SUCCESS` |
| Success body `data` | `{ "url": "https://checkout.stripe.com/..." }` |

**Request body** (`CreateCheckoutSessionRequest`):

```json
{
  "planId": "9fa85f64-5717-4562-b3fc-2c963f66afa6",
  "successUrl": "https://app.cobryx.mx/billing/success",
  "cancelUrl": "https://app.cobryx.mx/billing/cancel"
}
```

| HTTP | errorCode | When |
| --- | --- | --- |
| 400 | `VALIDATION.FAILED` | Missing/invalid fields |
| 404 | `DOMAIN.SUBSCRIPTION.PLAN_NOT_FOUND` | Unknown plan |

---

### 4. Create billing portal session (optional)

**`POST /api/v1/subscription/portal`**

| Item | Value |
| --- | --- |
| Success HTTP | `200 OK` |
| Success outcome | `BILLING.SUBSCRIPTION.PORTAL_CREATE_SUCCESS` |
| Request | `{ "returnUrl": "https://app.cobryx.mx/billing" }` |

---

### 5. List subscription plans (public)

**`GET /api/v1/subscription/plans`**

| Item | Value |
| --- | --- |
| Auth | Not required |
| Cache | `Cache-Control` — 1 hour |
| Success outcome | `BILLING.SUBSCRIPTION.PLANS_FETCH_SUCCESS` |

---

## Legacy / internal note

`UpgradeSubscriptionCommand` exists in `Cobryx.Application.Tenants.Commands.ManageSubscription` but is **not** exposed as `POST /api/v1/tenants/{tenantId}/subscription/upgrade`. Upgrades go through Stripe Checkout (`/subscription/checkout`) or the billing portal (`/subscription/portal`).

---

## k6 stress test mapping

| Scenario | HTTP | Endpoint |
| --- | --- | --- |
| Invoice stress | `POST` | `/invoices` |
| Upgrade stress | `POST` | `/subscription/checkout` |
| Mixed read | `GET` | `/subscription/status` |
| Mixed write | `POST` | `/invoices` |

**Operational thresholds** (from `monetization-stress.js`):

| Metric | Threshold |
| --- | --- |
| `http_req_failed` | `< 1%` (excluding accepted 403 on invoice create) |
| `http_req_duration` | `p(95) < 500ms` |

**Required environment variables**

| Variable | Description |
| --- | --- |
| `API_URL` | Base including `/api/v1`, e.g. `http://localhost:5142/api/v1` |
| `AUTH_TOKEN` | Bearer token for a seeded tenant user |
| `TENANT_ID` | Tenant GUID (must match token claim) |
| `PLAN_ID` | Target plan GUID for checkout scenario |
| `CUSTOMER_ID` | Existing customer GUID for invoice creation |

---

## Related code references

| Area | Location |
| --- | --- |
| Invoices API | `Cobryx.Api/Controllers/V1/InvoicesController.cs` |
| Subscription API | `Cobryx.Api/Controllers/V1/SubscriptionController.cs` |
| Plan enforcement | `Cobryx.Infrastructure/Services/SubscriptionEnforcementService.cs` |
| Outcome codes | `Cobryx.Api/Outcomes/InvoicingOutcomes.cs`, `Cobryx.Application/Subscriptions/Common/SubscriptionOutcomes.cs` |
| Error mapping | `Cobryx.Api/Errors/Mappers/ErrorMapper.cs`, `Cobryx.Api/Errors/Catalog/SubscriptionErrors.cs` |
