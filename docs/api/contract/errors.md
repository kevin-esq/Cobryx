# Error Contract

This document defines the standardized error codes used across the API. These codes are returned in the `errorCode` field of the `ApiErrorResponse`.

## Hierarchy
Errors are grouped logically within the domain.
Internal Usage: `Error.Module.Scenario`
API Return Value: `MODULE.SCENARIO` or `MODULE.ENTITY.SCENARIO`

---

## Authentication & Authorization
- **AUTH.NOT_AUTHENTICATED**: No valid credentials provided.
- **AUTH.TOKEN.INVALID**: Malformed or invalid signature.
- **AUTH.TOKEN.EXPIRED**: Token is past its validity period.
- **AUTH.INVALID_CREDENTIALS**: Wrong email or password.
- **AUTH.ACCOUNT_LOCKED**: Security lockout active.
- **AUTH.MFA.INVALID_CODE**: Verification failed.

---

## User Management
- **USER.NOT_FOUND**: User does not exist.
- **USER.EMAIL_ALREADY_EXISTS**: Conflict during registration.

---

## Customer (CRM)
- **CUSTOMER.NOT_FOUND**: Customer ID not found.
- **CUSTOMER.DUPLICATE**: Unique constraint violation.

---

## Invoicing & Payments
- **DOMAIN.INVOICING.INVOICE_NOT_FOUND**: Specific invoice missing.
- **DOMAIN.PAYMENT.NOT_FOUND**: Payment record missing.
- **DOMAIN.PAYMENT.TENANT_MISMATCH**: Security check failed.
- **DOMAIN.TAX.NOT_FOUND**: Tax configuration missing.

---

## Accounting (Ledger)
- **ACCOUNTING.JOURNAL_UNBALANCED**: Journal entry debits and credits do not net to zero.
- **ACCOUNTING.JOURNAL_IMMUTABLE**: Mutation attempted after post/seal.
- **ACCOUNTING.REVERSAL_AMOUNT_EXCEEDED**: Reversal amount exceeds reversible balance on original JE.
- **ACCOUNTING.SYSTEM_ACCOUNT_LOCKED**: System chart account cannot be renamed/re-coded.
- **ACCOUNTING.LEDGER_CHAIN_BROKEN**: Hash chain continuity failure.
- **ACCOUNTING.LEDGER_HASH_MISMATCH**: Sealed hash does not match recomputed value.

---

## Subscription & Plan Limits (Monetization)
- **DOMAIN.SUBSCRIPTION.LIMIT_REACHED** (HTTP 403, numeric 7001): Tenant exceeded a plan quota (e.g. monthly invoices). Thrown by plan enforcement before the write completes.
- **DOMAIN.SUBSCRIPTION.NOT_FOUND** (HTTP 404): No subscription record for the tenant.
- **DOMAIN.SUBSCRIPTION.EXPIRED** (HTTP 403): Subscription past validity; write operations blocked.
- **DOMAIN.SUBSCRIPTION.BLOCKED** (HTTP 403): Subscription administratively blocked.
- **DOMAIN.SUBSCRIPTION.PLAN_NOT_FOUND**: Referenced plan id does not exist.
- **DOMAIN.SUBSCRIPTION.DOWNGRADE_FORBIDDEN**: Usage exceeds target plan limits (internal upgrade command).
- **DOMAIN.TENANT.CONTEXT_MISSING**: `X-Tenant-Id` / token tenant context not resolved.

See [monetization.md](monetization.md) for endpoint-level mapping.

---

## Lending
- **DOMAIN.LOAN.NOT_FOUND**: Loan record missing.
- **DOMAIN.LOAN.ALREADY_CLOSED**: Operation not permitted on closed loan.
- **DOMAIN.INSTALLMENT.NOT_FOUND**: Installment missing.
- **DOMAIN.LOAN.AGREEMENT_SIGNED**: Agreement cannot be modified.

---

## System & Validation
- **SYSTEM.INTERNAL_ERROR**: Unexpected server failure.
- **SYSTEM.TOO_MANY_REQUESTS**: Rate limit exceeded.
- **VALIDATION.FAILED**: General validation error (see `validations.md`).
