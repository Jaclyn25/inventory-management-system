export type Category = { id: string; name: string }
export type Product = {
  id: string; sku: string; name: string; description: string; categoryId: string; categoryName: string;
  unitPrice: string; costPrice: string; lowStockThreshold: number; quantity: number; isArchived: boolean; version: number; createdAt: string;
}
export type Movement = { id: string; productId: string; sku: string; productName: string; kind: string; delta: number; quantityBefore: number; quantityAfter: number; costPrice: string; reason: string; createdAt: string }
export type Page<T> = { items: T[]; total: number; pageNumber: number; pageSize: number }
export type Report = {
  totalProducts: number; totalUnits: number; inventoryValue: string; retailValue: string; lowStockProducts: number; outOfStockProducts: number;
  categories: { categoryId: string; name: string; products: number; units: number; value: string }[];
  attentionProducts: Product[]; recentMovements: Movement[];
  activity: { date: string; received: number; issued: number; adjusted: number }[]; asOf: string; currency: string;
}
export class ApiError extends Error {
  constructor(message: string, public status: number) { super(message) }
}
export async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api${path}`, { ...init, headers: { 'Content-Type': 'application/json', ...init?.headers } })
  if (!response.ok) {
    const problem = await response.json().catch(() => ({}))
    const fields = problem.errors ? Object.values(problem.errors).flat().join(' ') : ''
    throw new ApiError(fields || problem.title || 'Request failed. Please try again.', response.status)
  }
  return response.status === 204 ? undefined as T : response.json()
}
export const json = (method: string, body: unknown): RequestInit => ({ method, body: JSON.stringify(body) })
export const params = (values: Record<string, string | number | boolean | undefined>) => new URLSearchParams(Object.entries(values).filter(([, value]) => value !== '' && value !== undefined).map(([key, value]) => [key, String(value)])).toString()
// Format decimal text without floating-point money arithmetic, including large totals.
export function money(value: string): string {
  const [whole, fraction = '00'] = value.split('.')
  return `$${BigInt(whole).toLocaleString('en-US')}.${fraction.padEnd(2, '0').slice(0, 2)}`
}
export const count = (value: number) => value.toLocaleString('en-US')
export const timestamp = (value: string) => new Date(value).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' })
export const kindLabel = (kind: string) => ({ in: 'Stock in', out: 'Stock out', adjustment: 'Adjustment' })[kind] ?? kind
