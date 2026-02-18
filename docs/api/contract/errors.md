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
