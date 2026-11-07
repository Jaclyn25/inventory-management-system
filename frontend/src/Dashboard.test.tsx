import { render, screen, within, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import { test, expect, vi } from 'vitest'
import userEvent from '@testing-library/user-event'
import Dashboard from './Dashboard'
import Reports from './Reports'
import type { Report } from './api'

const report: Report = { totalProducts: 2, totalUnits: 3, inventoryValue: '0.30', retailValue: '0.90', lowStockProducts: 1, outOfStockProducts: 1, categories: [{ categoryId: 'c1', name: 'Fictional supplies', units: 3, products: 2, value: '0.30' }], activity: [{ date: '2026-11-07', received: 3, issued: 0, adjusted: 0 }], recentMovements: [], attentionProducts: [], asOf: '2026-11-07T12:00:00Z', currency: 'USD' }
function mount(node: React.ReactNode) { const client = new QueryClient({ defaultOptions: { queries: { retry: false } } }); render(<QueryClientProvider client={client}><MemoryRouter>{node}</MemoryRouter></QueryClientProvider>) }
test('dashboard displays exact summary, alert counts and chart alternatives', async () => {
  vi.spyOn(globalThis, 'fetch').mockResolvedValue(Response.json(report))
  mount(<Dashboard notify={vi.fn()}/> )
  const value = await screen.findByText('Inventory value')
  expect(within(value.parentElement!).getByText('$0.30')).toBeVisible()
  expect(screen.getByText('1 low · 1 out of stock')).toBeVisible()
  expect(screen.getByRole('img', { name: /Category distribution: Fictional supplies, 3 units/ })).toBeVisible()
  expect(screen.getByRole('link', { name: 'View all movements' })).toHaveAttribute('href', '/movements')
})
test('report status filter requests filtered server totals', async () => {
  const fetch = vi.spyOn(globalThis, 'fetch').mockImplementation(async url => String(url).includes('/categories') ? Response.json([]) : String(url).includes('/products') ? Response.json({ items: [], total: 0, pageNumber: 1, pageSize: 10 }) : Response.json(report))
  mount(<Reports/> )
  expect(await screen.findByTestId('report-value')).toHaveTextContent('$0.30')
  await userEvent.setup().selectOptions(screen.getByLabelText('Report stock status'), 'out')
  await waitFor(() => expect(fetch.mock.calls.some(([url]) => String(url).includes('/reports?status=out'))).toBe(true))
})
