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
      "email": [
        {
          "errorCode": "VALIDATION.EMAIL.INVALID",
          "params": { "currentValue": "not-an-email" }
        }
      ],
      "password": [
        {
          "errorCode": "VALIDATION.PASSWORD.TOO_SHORT",
          "params": { "minLength": 8 }
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

## Common Validation Codes

### Required Fields
**Code Pattern:** `VALIDATION.{ENTITY}.{FIELD}.REQUIRED`
**Example:** `VALIDATION.USER.EMAIL.REQUIRED`

### Format Errors
**Code Pattern:** `VALIDATION.{ENTITY}.{FIELD}.INVALID_FORMAT`
**Example:** `VALIDATION.USER.EMAIL.INVALID`

### Length Constraints
**Code Pattern:** `VALIDATION.{ENTITY}.{FIELD}.TOO_SHORT` / `TOO_LONG`
**Params:**
- `minLength`: The minimum required length.
- `maxLength`: The maximum required length.

### Range Constraints
**Code Pattern:** `VALIDATION.{ENTITY}.{FIELD}.OUT_OF_RANGE`
**Params:**
- `min`: Minimum value.
- `max`: Maximum value.

---

## Frontend Handling

The frontend should map these codes to user-friendly messages using an i18n library.

**Example Mapping (en.json):**
```json
{
  "VALIDATION": {
    "USER": {
      "EMAIL": {
        "REQUIRED": "Email address is required.",
        "INVALID": "Please enter a valid email address."
      },
      "PASSWORD": {
        "TOO_SHORT": "Password must be at least {{minLength}} characters."
      }
    }
  }
}
```
