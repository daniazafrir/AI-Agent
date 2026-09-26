// Only normalize object keys. Serialized prompt JSON and tool text stay untouched.
export function normalizeTraceKeys(value: any): any {
  if (Array.isArray(value)) return value.map(normalizeTraceKeys);
  if (value !== null && typeof value === 'object') {
    return Object.fromEntries(Object.entries(value).map(([key, item]) =>
      [key.charAt(0).toLowerCase() + key.slice(1), normalizeTraceKeys(item)]));
  }
  return value;
}
