# Outcomes Contract

This document defines the possible success outcomes for API operations. These codes are returned in the `outcome` field of the API response.

## Pattern
All outcome codes follow the hierarchy: `MODULE.ENTITY.ACTION_RESULT`
Example: `CRM.CUSTOMER.CREATED_SUCCESS`

---

## Authentication Outcomes

### AUTH.USER.LOGIN_SUCCESS
**Endpoint:** POST /api/v1/auth/login
**HTTP Status:** 200

#### Meaning: Login
The user has successfully logged in and a session has been established.

---

### AUTH.USER.LOGIN_MFA_REQUIRED
**Endpoint:** POST /api/v1/auth/login
**HTTP Status:** 200

#### Meaning: MFA Required
Primary credentials are valid, but MFA is required to complete the login.

---

### AUTH.USER.SIGNUP_SUCCESS
**Endpoint:** POST /api/v1/auth/signup
**HTTP Status:** 201

#### Meaning: Signup
A new user account has been successfully created.

---

### AUTH.USER.SIGNUP_VERIFICATION_REQUIRED
**Endpoint:** POST /api/v1/auth/signup
**HTTP Status:** 200

#### Meaning: Verification Required
Account created, but email verification is required.

---

## Customer Outcomes

### CRM.CUSTOMER.CREATED_SUCCESS
**Endpoint:** POST /api/v1/customers
**HTTP Status:** 201

#### Meaning: Created
A new customer record has been created.

---

### CRM.CUSTOMER.UPDATE_SUCCESS
**Endpoint:** PUT /api/v1/customers/{id}
**HTTP Status:** 200

#### Meaning: Update
Customer details have been updated.

---

### CRM.CUSTOMER.DELETE_SUCCESS
**Endpoint:** DELETE /api/v1/customers/{id}
**HTTP Status:** 200

---

## Invoicing Outcomes

### BILLING.INVOICE.CREATE_SUCCESS
**Endpoint:** POST /api/v1/invoices
**HTTP Status:** 201

#### Meaning
A new invoice was created and persisted. See [monetization.md](monetization.md) for plan-limit behavior.

---

### BILLING.INVOICE.SEARCH_SUCCESS
**Endpoint:** GET /api/v1/invoices
**HTTP Status:** 200

---

### BILLING.INVOICE.PAYMENT_SUCCESS
### BILLING.TAX.SEARCH_SUCCESS

---

## Lending Outcomes

### LENDING.LOAN.CREATE_SUCCESS
### LENDING.LOAN.PAYMENT_SUCCESS

---

## Subscription Outcomes

### BILLING.SUBSCRIPTION.PLANS_FETCH_SUCCESS
**Endpoint:** GET /api/v1/subscription/plans
**HTTP Status:** 200

#### Meaning
Available subscription plans retrieved (public, cacheable).

---

### BILLING.SUBSCRIPTION.STATUS_CHECK_SUCCESS
**Endpoint:** GET /api/v1/subscription/status
**HTTP Status:** 200

#### Meaning
Current tenant subscription status and entitlements retrieved.

---

### BILLING.SUBSCRIPTION.CHECKOUT_CREATE_SUCCESS
**Endpoint:** POST /api/v1/subscription/checkout
**HTTP Status:** 200

#### Meaning
Stripe Checkout URL generated for plan upgrade or purchase.

---

### BILLING.SUBSCRIPTION.PORTAL_CREATE_SUCCESS
**Endpoint:** POST /api/v1/subscription/portal
**HTTP Status:** 200

#### Meaning
Stripe Customer Portal URL generated for subscription self-service.

---

### BILLING.SUBSCRIPTION.INTELLIGENCE_SUCCESS
**Endpoint:** GET /api/v1/subscription/intelligence
**HTTP Status:** 200

---

### BILLING.SUBSCRIPTION.SYNC_SUCCESS
**Endpoint:** POST /api/v1/system/commands/sync-subscription
**HTTP Status:** 200

---

## Tenant Outcomes

### TENANT.ADMIN.BRANDING_UPDATE_SUCCESS
### TENANT.ADMIN.SETTINGS_UPDATE_SUCCESS
### TENANT.ADMIN.ONBOARDING_SUCCESS
