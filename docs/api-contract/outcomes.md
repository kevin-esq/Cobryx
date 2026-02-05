# Outcomes Contract

This document defines the possible success outcomes for API operations. These codes are returned in the `outcome` field of the API response.

## Authentication Outcomes

### AUTH.LOGIN.COMPLETED
**Endpoint:** POST /api/auth/login
**HTTP Status:** 200

#### Meaning
The user has successfully logged in and a session has been established.

#### Expected Frontend Behavior
- Store the returned `token` and `refreshToken` (if provided).
- Redirect the user to the dashboard or intended page.

---

### AUTH.LOGIN.MFA_REQUIRED
**Endpoint:** POST /api/auth/login
**HTTP Status:** 200

#### Meaning
Primary credentials (email/password) are valid, but Multi-Factor Authentication is required to complete the login.

#### Expected Frontend Behavior
- Redirect user to the MFA verification screen.
- Pass the temporary session identifier if required by the MFA endpoint.

---

### AUTH.SIGNUP.CREATED
**Endpoint:** POST /api/auth/register
**HTTP Status:** 201

#### Meaning
A new user account has been successfully created.

#### Expected Frontend Behavior
- Notify the user of success.
- Redirect to login or verification page depending on `AUTH.SIGNUP.VERIFICATION_REQUIRED`.

---

### AUTH.SIGNUP.VERIFICATION_REQUIRED
**Endpoint:** POST /api/auth/register
**HTTP Status:** 200

#### Meaning
Account created, but email verification is required before login is allowed.

#### Expected Frontend Behavior
- Show a message instructing the user to check their email.

---

### AUTH.VERIFICATION_EMAIL_SENT
**Endpoint:** POST /api/auth/resend-verification
**HTTP Status:** 200

#### Meaning
A new verification email has been dispatched.

---

### AUTH.EMAIL_VERIFIED
**Endpoint:** POST /api/auth/verify-email
**HTTP Status:** 200

#### Meaning
The user's email address has been successfully verified.

---

### AUTH.PASSWORD_RESET_REQUESTED
**Endpoint:** POST /api/auth/forgot-password
**HTTP Status:** 200

#### Meaning
If the account exists, a password reset link has been sent.

#### Notes
For security reasons, this outcome is returned even if the email does not exist in the system.

---

### AUTH.PASSWORD_CHANGED
**Endpoint:** POST /api/auth/reset-password
**HTTP Status:** 200

#### Meaning
The user's password has been successfully updated.

---

### AUTH.MFA.ENABLED
**Endpoint:** POST /api/auth/mfa/enable
**HTTP Status:** 200

#### Meaning
MFA has been enabled for the user account.

---

### AUTH.MFA.VERIFIED
**Endpoint:** POST /api/auth/mfa/verify
**HTTP Status:** 200

#### Meaning
The provided MFA code was valid.

---

### AUTH.FIDO2.REGISTERED
**Endpoint:** POST /api/auth/fido2/register
**HTTP Status:** 200

#### Meaning
A new FIDO2 (WebAuthn) credential has been registered.

---

### AUTH.SESSION.REVOKED
**Endpoint:** POST /api/auth/logout
**HTTP Status:** 200

#### Meaning
The user session has been invalidated.

---

## Customer Outcomes

### CUSTOMER.CREATED
**Endpoint:** POST /api/customers
**HTTP Status:** 201

#### Meaning
A new customer record has been created.

---

### CUSTOMER.UPDATED
**Endpoint:** PUT /api/customers/{id}
**HTTP Status:** 200

#### Meaning
Customer details have been updated.

---

### CUSTOMER.DELETED
**Endpoint:** DELETE /api/customers/{id}
**HTTP Status:** 200

#### Meaning
The customer record has been soft-deleted or removed.

---

### CUSTOMER.SEARCH.COMPLETED
**Endpoint:** GET /api/customers
**HTTP Status:** 200

#### Meaning
The search query was executed successfully (even if zero results were found).

---

## Tenant Outcomes

### TENANT.BRANDING.UPDATED
**Endpoint:** PUT /api/tenant/branding
**HTTP Status:** 200

#### Meaning
Tenant branding settings (logo, colors) have been updated.

---

### TENANT.SETTINGS.UPDATED
**Endpoint:** PUT /api/tenant/settings
**HTTP Status:** 200

#### Meaning
General tenant configuration has been updated.
