import type { DictionaryShape } from "./es";

export const en: DictionaryShape = {
  common: {
    brand: "Cobryx",
    tagline: "Financial platform for intelligent collections.",
    languageLabel: "Language",
    languageOptions: {
      es: "Spanish",
      en: "English"
    },
    themeLabel: "Theme",
    themeOptions: {
      light: "Light",
      dark: "Dark",
      system: "System"
    }
  },
  errors: {
    fallback: "Something went wrong. Please try again.",
    network: "We couldn't reach the server. Check your connection.",
    serverError: "The server had a problem. Retry in a few seconds.",
    unauthorized: "Your session expired. Please sign in again.",
    forbidden: "You don't have permission to perform this action.",
    notFound: "We couldn't find what you were looking for.",
    rateLimited: "Too many attempts. Wait a moment before retrying.",
    validation: "Please review the form."
  },
  auth: {
    login: {
      eyebrow: "Cobryx",
      title: "Sign in",
      description:
        "Connect to your workspace to manage customers, credits and payments.",
      emailLabel: "Email",
      emailPlaceholder: "you@company.com",
      passwordLabel: "Password",
      passwordPlaceholder: "••••••••",
      submit: "Sign in",
      submitting: "Signing in...",
      empty: "Enter both email and password.",
      scaffoldNotice:
        "Initial scaffold. Real authentication lands in roadmap item F1."
    }
  },
  dashboard: {
    workspaceLabel: "Cobryx",
    title: "Workspace",
    signOut: "Sign out",
    scaffoldHeadline: "Scaffold ready",
    scaffoldDescription:
      "This is the dashboard shell. Real screens land in roadmap items F1–F3.",
    comingSoon: "Coming soon",
    modules: {
      customers: {
        title: "Customers",
        description: "Browse and manage tenant customers."
      },
      credits: {
        title: "Credits",
        description: "Origination, scoring and follow-up."
      },
      payments: {
        title: "Payments",
        description: "Application, refunds and reconciliation."
      },
      invoicing: {
        title: "Invoicing",
        description: "Invoices, due dates and aging."
      },
      settings: {
        title: "Settings",
        description: "Workspace, branding and subscription."
      }
    }
  },
  apiErrors: {
    "AUTH.LOGIN.INVALID_CREDENTIALS":
      "Invalid credentials. Verify your email and password.",
    "AUTH.LOGIN.ACCOUNT_LOCKED":
      "Your account is temporarily locked due to too many attempts.",
    "AUTH.LOGIN.EMAIL_NOT_VERIFIED":
      "Verify your email before signing in.",
    "AUTH.LOGIN.MFA_REQUIRED":
      "Continue with the second authentication factor.",
    "AUTH.SIGNUP.EMAIL_TAKEN": "That email is already registered.",
    "AUTH.SIGNUP.VERIFICATION_REQUIRED":
      "We sent you a verification email.",
    "AUTH.PASSWORD.RESET_LINK_SENT":
      "If the email exists, we sent a reset link.",
    "AUTH.PASSWORD.TOO_WEAK":
      "The password is too weak. Use at least 8 characters mixing upper, lower and digits.",
    "AUTH.TOKEN.INVALID": "The link is invalid or already used.",
    "AUTH.TOKEN.EXPIRED": "The link expired. Request a new one.",
    "AUTH.MFA.INVALID_CODE": "The verification code is incorrect.",
    "AUTH.VERIFICATION_EMAIL_SENT":
      "We sent a new verification email.",
    "TENANT.SUBSCRIPTION.INACTIVE":
      "The tenant subscription is not active.",
    "TENANT.NOT_FOUND": "Tenant not found.",
    "COMMON.VALIDATION_FAILED": "Please review the submitted data.",
    "COMMON.RATE_LIMIT_EXCEEDED":
      "Too many attempts. Wait a few seconds before retrying.",
    "COMMON.SERVER_ERROR": "The server had a problem."
  }
};
