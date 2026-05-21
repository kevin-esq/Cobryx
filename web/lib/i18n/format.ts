export type InterpolationVars = Record<string, string | number | boolean>;

const PATTERN = /\{(\w+)\}/g;

export function interpolate(template: string, vars?: InterpolationVars): string {
  if (!vars) return template;
  return template.replace(PATTERN, (_, key: string) => {
    const value = vars[key];
    return value === undefined || value === null ? `{${key}}` : String(value);
  });
}

/**
 * Resolve a nested dictionary key path like "auth.login.title".
 * Returns the matched value if it is a string, otherwise undefined.
 */
export function resolveKey(root: unknown, path: string): string | undefined {
  if (root == null || typeof root !== "object") return undefined;

  const segments = path.split(".");
  let current: unknown = root;

  for (const segment of segments) {
    if (current == null || typeof current !== "object") return undefined;
    current = (current as Record<string, unknown>)[segment];
  }

  return typeof current === "string" ? current : undefined;
}
