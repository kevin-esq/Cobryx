const requirePublic = (key: string, value: string | undefined): string => {
  if (!value) {
    throw new Error(
      `Missing required public env var "${key}". Did you copy web/.env.example to web/.env.local?`
    );
  }
  return value;
};

export const publicEnv = {
  apiUrl: requirePublic(
    "NEXT_PUBLIC_API_URL",
    process.env.NEXT_PUBLIC_API_URL
  )
} as const;

export const serverEnv = {
  apiUrl: process.env.API_URL ?? process.env.NEXT_PUBLIC_API_URL ?? ""
} as const;
