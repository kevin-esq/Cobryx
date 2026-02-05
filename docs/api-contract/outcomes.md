# API Outcome Contract

This document defines the stable, machine-readable outcome codes returned by the Cobryx API. These codes are used by the frontend to determine application flow and display appropriate localized messages.

## Zero-Text Policy
The Cobryx API follows a strict **Zero-Text Policy** for success responses. The `message` field in the response is optional and usually `null`. The `outcomeCode` is the primary indicator of the operation's result.

## State Taxonomy
All outcome codes follow the pattern: `{CONTEXT}.{DOMAIN}.{STATE}`

| State | Description |
| :--- | :--- |
| `COMPLETED` | The operation was fully successful and is finished. |
| `CREATED` | A new resource was successfully created. |
| `UPDATED` | An existing resource was successfully updated. |
| `DELETED` | A resource was successfully removed. |
| `REQUIRED` | The flow is blocked awaiting further user action (e.g., MFA). |
| `PENDING` | The operation has started but is awaiting background processing or external confirmation. |
| `SEARCH.COMPLETED` | A search or list operation was successful. |

## Outcome Catalogs

### Authentication (`AUTH`)
| Code | Meaning |
| :--- | :--- |
| `AUTH.LOGIN.COMPLETED` | Login successful, JWT issued. |
| `AUTH.LOGIN.MFA_REQUIRED` | Credentials valid, but MFA is required to complete. |
| `AUTH.SIGNUP.CREATED` | Account created successfully. |
| `AUTH.SIGNUP.VERIFICATION_REQUIRED` | Account created, but email verification is pending. |
| `AUTH.EMAIL_VERIFIED` | Email address was successfully verified. |
| `AUTH.MFA.ENABLED` | MFA has been enabled for the user. |
| `AUTH.MFA.VERIFIED` | MFA verification (TOTP/FIDO2) was successful. |
| `AUTH.FIDO2.REGISTERED` | A new passkey was successfully registered. |

### Customers (`CUSTOMER`)
| Code | Meaning |
| :--- | :--- |
| `CUSTOMER.CREATED` | Customer created. |
| `CUSTOMER.UPDATED` | Customer updated. |
| `CUSTOMER.DELETED` | Customer deleted. |
| `CUSTOMER.SEARCH.COMPLETED` | Search results returned. |

### Financial (`FINANCIAL`)
| Code | Meaning |
| :--- | :--- |
| `FINANCIAL.PAYMENT.REGISTERED` | Payment was successfully registered. |
| `FINANCIAL.TAX.CREATED` | Tax configuration created. |
| `FINANCIAL.INVOICE.CREATED` | Invoice generated. |

*(Note: This list is not exhaustive and is updated as the API evolves.)*
