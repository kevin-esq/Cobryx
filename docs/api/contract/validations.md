# Validation Contract

This document defines the structure and standards for validation errors.

## General Structure

When a `VALIDATION.FAILED` error occurs (HTTP 400), the response `metadata` will contain an `errors` object. 

### Response Format

```json
{
  "errorCode": "VALIDATION.FAILED",
  "numericCode": 1001,
  "metadata": {
    "errors": {
      "firstName": [
        {
          "errorCode": "VALIDATION.CUSTOMER.FIRST_NAME.REQUIRED",
          "params": { }
        }
      ],
      "phone": [
        {
          "errorCode": "VALIDATION.CUSTOMER.PHONE.INVALID",
          "params": { "currentValue": "123" }
        }
      ]
    }
  }
}
```

The `errors` object is a map where:
- **Key**: The name of the field that failed validation (camelCase).
- **Value**: An array of error objects describing the specific failures.

---

## Hierarchy Pattern
Validation codes are hierarchical to allow granular frontend mapping:
`VALIDATION.{MODULE}.{FIELD}.{REASON}`

### Common Patterns:
- **REQUIRED**: `VALIDATION.CUSTOMER.FIRST_NAME.REQUIRED`
- **INVALID**: `VALIDATION.CUSTOMER.PHONE.INVALID`
- **TOO_LONG**: `VALIDATION.CUSTOMER.FIRST_NAME.TOO_LONG`
- **TOO_SHORT**: `VALIDATION.PASSWORD.TOO_SHORT`

---

## Frontend Handling

The frontend should map these codes to user-friendly messages using an i18n library.

**Example Mapping (en.json):**
```json
{
  "VALIDATION": {
    "CUSTOMER": {
      "FIRST_NAME": {
        "REQUIRED": "First name is required.",
        "TOO_LONG": "First name cannot exceed 100 characters."
      },
      "PHONE": {
        "INVALID": "Please enter a valid phone number."
      }
    }
  }
}
```
