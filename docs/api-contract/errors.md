# Error Contract

This document defines the standardized error codes used across the API. These codes are returned in the `errorCode` field of the `ApiErrorResponse`.

## Authentication Errors

### AUTH.INVALID_CREDENTIALS
**HTTP Status:** 401
**Numeric Code:** 1101

#### Meaning
The provided email or password is incorrect.

#### Expected Frontend Behavior
- Clear the password field.
- Show a generic "Invalid credentials" error message.

---

### AUTH.ACCOUNT_LOCKED
**HTTP Status:** 403
**Numeric Code:** 1102

#### Meaning
The account has been locked due to too many failed login attempts or administrative action.

---

### AUTH.EMAIL_NOT_VERIFIED
**HTTP Status:** 403
**Numeric Code:** 1103

#### Meaning
Login is prevented because the email address has not been verified.

#### Expected Frontend Behavior
- Prompt the user to check their email or request a new verification link.

---

### AUTH.TOKEN.EXPIRED
**HTTP Status:** 401
**Numeric Code:** 1104

#### Meaning
The provided access token or refresh token has expired.

#### Expected Frontend Behavior
- Attempt to refresh the token.
- If refresh fails, redirect to login.

---

### AUTH.NOT_AUTHENTICATED
**HTTP Status:** 401
**Numeric Code:** 1105

#### Meaning
No valid authentication token was provided.

---

### AUTH.TOKEN.INVALID
**HTTP Status:** 401
**Numeric Code:** 1106

#### Meaning
The provided token is malformed or invalid signature.

---

### AUTH.TOKEN.COMPROMISED
**HTTP Status:** 403
**Numeric Code:** 1107

#### Meaning
Security system detected potential token reuse or compromise.

#### Expected Frontend Behavior
- Force immediate logout.
- Warn user of security event.

---

### AUTH.SESSION.REVOKED
**HTTP Status:** 401
**Numeric Code:** 1108

#### Meaning
The specific session associated with the token has been explicitly revoked.

---

## User Errors

### USER.NOT_FOUND
**HTTP Status:** 404
**Numeric Code:** 4001

#### Meaning
The requested user ID does not exist.

---

### USER.EMAIL_ALREADY_EXISTS
**HTTP Status:** 409
**Numeric Code:** 4002

#### Meaning
Registration failed because the email address is already in use by another account.

---

### USER.NOT_REGISTERED
**HTTP Status:** 404
**Numeric Code:** 4003

#### Meaning
The user is not registered in the current context/tenant.

---

## Customer Errors

### CUSTOMER.NOT_FOUND
**HTTP Status:** 404
**Numeric Code:** [From Catalog]

#### Meaning
The requested customer ID was not found.

---

### CUSTOMER.DUPLICATE
**HTTP Status:** 409
**Numeric Code:** [From Catalog]

#### Meaning
A customer with the same unique identifiers (e.g., email / government ID) already exists.

---

## Tenant Errors

### TENANT.NOT_FOUND
**HTTP Status:** 404
**Numeric Code:** [From Catalog]

#### Meaning
The requested tenant ID was not found.

---

### TENANT.CONTEXT_MISSING
**HTTP Status:** 400
**Numeric Code:** [From Catalog]

#### Meaning
The request requires a tenant context (e.g., `X-Tenant-ID` header) but none was provided.

---

### TENANT.ONBOARDING_REQUIRED
**HTTP Status:** 403
**Numeric Code:** [From Catalog]

#### Meaning
Tenant exists but has not completed the required onboarding steps.

---

### TENANT.ONBOARDING_COMPLETED
**HTTP Status:** 409
**Numeric Code:** [From Catalog]

#### Meaning
Attempted to perform onboarding steps on a tenant that is already active.

---

## System Errors

### SYSTEM.INTERNAL_ERROR
**HTTP Status:** 500
**Numeric Code:** 1000

#### Meaning
An unexpected server-side error occurred.

---

### SYSTEM.TOO_MANY_REQUESTS
**HTTP Status:** 429
**Numeric Code:** 1090

#### Meaning
Rate limit exceeded.

---

### VALIDATION.FAILED
**HTTP Status:** 400
**Numeric Code:** 1001

#### Meaning
The request failed validation. See `validations.md` for details on the error structure.
