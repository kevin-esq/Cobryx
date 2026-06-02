export const es = {
  common: {
    brand: "Cobryx",
    tagline: "Plataforma financiera para cobranza inteligente.",
    languageLabel: "Idioma",
    languageOptions: {
      es: "Español",
      en: "Inglés"
    },
    themeLabel: "Tema",
    themeOptions: {
      light: "Claro",
      dark: "Oscuro",
      system: "Sistema"
    }
  },
  errors: {
    fallback: "Algo salió mal. Inténtalo de nuevo.",
    network: "No pudimos conectar con el servidor. Revisa tu conexión.",
    serverError: "El servidor tuvo un problema. Reintenta en unos segundos.",
    unauthorized: "Tu sesión expiró. Vuelve a iniciar sesión.",
    forbidden: "No tienes permisos para esta acción.",
    notFound: "No encontramos lo que buscas.",
    rateLimited: "Demasiados intentos. Espera un momento antes de reintentar.",
    validation: "Revisa los datos del formulario."
  },
  auth: {
    login: {
      eyebrow: "Cobryx",
      title: "Inicia sesión",
      description:
        "Conecta con tu workspace para administrar clientes, créditos y pagos.",
      emailLabel: "Correo",
      emailPlaceholder: "tu@empresa.com",
      passwordLabel: "Contraseña",
      passwordPlaceholder: "••••••••",
      submit: "Entrar",
      submitting: "Entrando...",
      empty: "Captura correo y contraseña."
    },
    mfa: {
      title: "Verificación en dos pasos",
      description: "Ingresa el código de tu app de autenticación o usa tu llave de seguridad.",
      totpLabel: "Código de 6 dígitos",
      totpPlaceholder: "000000",
      totpSubmit: "Verificar código",
      totpSubmitting: "Verificando...",
      fido2Button: "Usar llave de seguridad",
      fido2Submitting: "Esperando llave...",
      orDivider: "o",
      backToLogin: "Volver al inicio de sesión"
    },
    sessions: {
      title: "Sesiones activas",
      description: "Dispositivos donde tu cuenta está conectada.",
      device: "Dispositivo",
      ip: "IP",
      lastActive: "Última actividad",
      current: "Sesión actual",
      revoke: "Revocar",
      revoking: "Revocando...",
      revokeAll: "Cerrar todas las sesiones",
      revokeAllSubmitting: "Cerrando...",
      empty: "No hay sesiones activas.",
      loadError: "No pudimos cargar las sesiones.",
      backToDashboard: "Volver al workspace"
    }
  },
  dashboard: {
    signOut: "Cerrar sesión",
    sessionsLink: "Sesiones activas",
    workspaceLabel: "Cobryx",
    title: "Workspace",
    scaffoldHeadline: "Scaffold listo",
    scaffoldDescription:
      "Este es el cascarón del dashboard. Las pantallas reales se conectan en los items F1–F3 del roadmap.",
    comingSoon: "Próximamente",
    modules: {
      customers: {
        title: "Clientes",
        description: "Consulta y administra clientes del tenant."
      },
      credits: {
        title: "Créditos",
        description: "Originación, scoring y seguimiento."
      },
      payments: {
        title: "Pagos",
        description: "Aplicación, reembolsos y conciliación."
      },
      invoicing: {
        title: "Facturación",
        description: "Facturas, vencimientos y aging."
      },
      settings: {
        title: "Configuración",
        description: "Workspace, branding y suscripción."
      }
    }
  },
  // Back-end API error codes, conventionally `MODULE.SUBMODULE.CODE`.
  // Extend as the contract grows. Missing codes fall back to errors.fallback.
  apiErrors: {
    "AUTH.LOGIN.INVALID_CREDENTIALS":
      "Credenciales inválidas. Verifica tu correo y contraseña.",
    "AUTH.LOGIN.ACCOUNT_LOCKED":
      "Tu cuenta está bloqueada temporalmente por demasiados intentos.",
    "AUTH.LOGIN.EMAIL_NOT_VERIFIED":
      "Confirma tu correo antes de iniciar sesión.",
    "AUTH.LOGIN.MFA_REQUIRED": "Continúa con el segundo factor de autenticación.",
    "AUTH.SIGNUP.EMAIL_TAKEN": "Ese correo ya está registrado.",
    "AUTH.SIGNUP.VERIFICATION_REQUIRED":
      "Te enviamos un correo para verificar tu cuenta.",
    "AUTH.PASSWORD.RESET_LINK_SENT":
      "Si el correo existe, te enviamos un enlace para restablecer tu contraseña.",
    "AUTH.PASSWORD.TOO_WEAK":
      "La contraseña es muy débil. Usa al menos 8 caracteres con mayúsculas, minúsculas y números.",
    "AUTH.TOKEN.INVALID": "El enlace es inválido o ya se usó.",
    "AUTH.TOKEN.EXPIRED": "El enlace expiró. Solicita uno nuevo.",
    "AUTH.MFA.INVALID_CODE": "El código de verificación es incorrecto.",
    "AUTH.VERIFICATION_EMAIL_SENT":
      "Enviamos un nuevo correo de verificación.",
    "TENANT.SUBSCRIPTION.INACTIVE":
      "La suscripción del tenant no está activa.",
    "TENANT.NOT_FOUND": "No encontramos el tenant.",
    "COMMON.VALIDATION_FAILED": "Revisa los datos enviados.",
    "COMMON.RATE_LIMIT_EXCEEDED":
      "Demasiados intentos. Espera unos segundos antes de reintentar.",
    "COMMON.SERVER_ERROR": "Hubo un problema en el servidor."
  }
};

export type DictionaryShape = typeof es;
