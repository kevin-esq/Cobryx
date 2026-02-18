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

#### Meaning
The user has successfully logged in and a session has been established.

---

### AUTH.USER.LOGIN_MFA_REQUIRED
**Endpoint:** POST /api/v1/auth/login
**HTTP Status:** 200

#### Meaning
Primary credentials are valid, but MFA is required to complete the login.

---

### AUTH.USER.SIGNUP_SUCCESS
**Endpoint:** POST /api/v1/auth/signup
**HTTP Status:** 201

#### Meaning
A new user account has been successfully created.

---

### AUTH.USER.SIGNUP_VERIFICATION_REQUIRED
**Endpoint:** POST /api/v1/auth/signup
**HTTP Status:** 200

#### Meaning
Account created, but email verification is required.

---

## Customer Outcomes

### CRM.CUSTOMER.CREATED_SUCCESS
**Endpoint:** POST /api/v1/customers
**HTTP Status:** 201

#### Meaning
A new customer record has been created.

---

### CRM.CUSTOMER.UPDATE_SUCCESS
**Endpoint:** PUT /api/v1/customers/{id}
**HTTP Status:** 200

#### Meaning
Customer details have been updated.

---

### CRM.CUSTOMER.DELETE_SUCCESS
**Endpoint:** DELETE /api/v1/customers/{id}
**HTTP Status:** 200

---

## Invoicing Outcomes

### BILLING.INVOICE.CREATE_SUCCESS
### BILLING.INVOICE.PAYMENT_SUCCESS
### BILLING.TAX.SEARCH_SUCCESS

---

## Lending Outcomes

### LENDING.LOAN.CREATE_SUCCESS
### LENDING.LOAN.PAYMENT_SUCCESS

---

## Subscription Outcomes

### BILLING.SUBSCRIPTION.CHECKOUT_CREATE_SUCCESS
### BILLING.SUBSCRIPTION.PORTAL_CREATE_SUCCESS

---

## Tenant Outcomes

### TENANT.ADMIN.BRANDING_UPDATE_SUCCESS
### TENANT.ADMIN.SETTINGS_UPDATE_SUCCESS
### TENANT.ADMIN.ONBOARDING_SUCCESS
