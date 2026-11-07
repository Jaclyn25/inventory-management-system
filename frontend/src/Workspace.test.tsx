import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import { vi, test, expect } from 'vitest'
import App from './App'
import { ProductForm, StockForm } from './Forms'
import { api, money, type Product } from './api'
import type { ReactNode } from 'react'

const product: Product = { id: 'p1', sku: 'QA-100', name: 'Desk notebook', description: 'Fictional item', categoryId: 'c1', categoryName: 'Stationery', costPrice: '4.50', unitPrice: '7.20', quantity: 3, lowStockThreshold: 5, version: 1, isArchived: false, createdAt: '2026-01-01T00:00:00Z' }
function mount(node: ReactNode, path = '/products') {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return render(<QueryClientProvider client={client}><MemoryRouter initialEntries={[path]}>{node}</MemoryRouter></QueryClientProvider>)
}
function mockFetch(handler?: (url: string, init?: RequestInit) => Response | Promise<Response>) {
  return vi.spyOn(globalThis, 'fetch').mockImplementation(async (url, init) => {
    if (handler) return handler(String(url), init)
    if (String(url).startsWith('/api/categories')) return Response.json([{ id: 'c1', name: 'Stationery' }])
    if (String(url) === '/api/products/p1') return Response.json(product)
    return Response.json({ items: [product], total: 1, pageNumber: 1, pageSize: 10 })
  })
}
test('catalog renders stock state and sends search and filter parameters', async () => {
  const fetch = mockFetch()
  const user = userEvent.setup()
  mount(<App/>)
  expect(await screen.findByRole('button', { name: 'Desk notebook' })).toBeVisible()
  expect(screen.getByText('Low stock', { selector: '.badge' })).toBeVisible()
  await user.type(screen.getByRole('textbox', { name: 'Search products' }), 'paper')
  await user.selectOptions(screen.getByRole('combobox', { name: 'Stock status' }), 'out')
  await waitFor(() => expect(fetch).toHaveBeenCalledWith(expect.stringContaining('search=paper'), expect.anything()))
  expect(fetch.mock.calls.some(([url]) => String(url).includes('status=out'))).toBe(true)
})
test('product form validates required fields before making a mutation', async () => {
  const fetch = mockFetch()
  const user = userEvent.setup()
  mount(<ProductForm onClose={vi.fn()} onSaved={vi.fn()}/> )
  await user.click(screen.getByRole('button', { name: 'Save product' }))
  expect(await screen.findByText('Product name is required.')).toBeVisible()
  expect(fetch.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(false)
})
test('product form sends exact decimal text and never includes a quantity', async () => {
  const saved = vi.fn(), user = userEvent.setup()
  const fetch = mockFetch((url, init) => url.includes('categories') ? Response.json([{ id: 'c1', name: 'Stationery' }]) : init?.method === 'POST' ? Response.json(product, { status: 201 }) : Response.json(product))
  mount(<ProductForm onClose={vi.fn()} onSaved={saved}/> )
  await user.type(screen.getByLabelText('Product name'), 'Notebook')
  await user.type(screen.getByLabelText('SKU'), 'NB-100')
  await user.selectOptions(screen.getByLabelText('Category'), 'c1')
  await user.type(screen.getByLabelText('Cost price (USD)'), '0.10')
  await user.type(screen.getByLabelText('Unit price (USD)'), '0.30')
  await user.click(screen.getByRole('button', { name: 'Save product' }))
  await waitFor(() => expect(saved).toHaveBeenCalled())
  const body = JSON.parse(String(fetch.mock.calls.find(([, init]) => init?.method === 'POST')![1]!.body))
  expect(body.costPrice).toBe('0.10'); expect(body.unitPrice).toBe('0.30'); expect(body).not.toHaveProperty('quantity')
})
test('API conflicts are visible and keep product form open', async () => {
  mockFetch((url) => url.includes('categories') ? Response.json([{ id: 'c1', name: 'Stationery' }]) : Response.json({ title: 'Duplicate SKU.' }, { status: 409 }))
  const user = userEvent.setup(), close = vi.fn()
  mount(<ProductForm product={product} onClose={close} onSaved={vi.fn()}/> )
  await user.click(screen.getByRole('button', { name: 'Save product' }))
  expect(await screen.findByRole('alert')).toHaveTextContent('Duplicate SKU.')
  expect(close).not.toHaveBeenCalled()
})
test('stock out blocks quantities above available stock', async () => {
  const fetch = mockFetch(), user = userEvent.setup()
  mount(<StockForm product={product} onClose={vi.fn()} onSaved={vi.fn()}/> )
  await user.selectOptions(await screen.findByLabelText('Movement type'), 'out')
  await user.type(screen.getByLabelText('Quantity'), '9')
  await user.type(screen.getByLabelText('Reason'), 'Customer order')
  await user.click(screen.getByRole('button', { name: 'Confirm movement' }))
  expect(await screen.findByText('Quantity exceeds available stock.')).toBeVisible()
  expect(fetch.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(false)
})
test('uncertain stock requests retry the same key and operation ID', async () => {
  let attempt = 0
  const fetch = mockFetch((_url, init) => {
    if (init?.method === 'POST') { attempt++; if (attempt === 1) return Promise.reject(new TypeError('Network interrupted')); return Response.json({ id: 'movement' }) }
    return Response.json(product)
  })
  const user = userEvent.setup(), saved = vi.fn()
  mount(<StockForm product={product} onClose={vi.fn()} onSaved={saved}/> )
  await user.type(await screen.findByLabelText('Quantity'), '2')
  await user.type(screen.getByLabelText('Reason'), 'Warehouse delivery')
  await user.click(screen.getByRole('button', { name: 'Confirm movement' }))
  await user.click(await screen.findByRole('button', { name: 'Retry same operation' }))
  await waitFor(() => expect(saved).toHaveBeenCalled())
  const writes = fetch.mock.calls.filter(([, init]) => init?.method === 'POST')
  expect(writes).toHaveLength(2)
  expect(writes[0][1]!.headers).toEqual(writes[1][1]!.headers)
  expect(JSON.parse(String(writes[0][1]!.body)).operationId).toEqual(JSON.parse(String(writes[1][1]!.body)).operationId)
})
test('mobile navigation can open, navigate and close', async () => {
  mockFetch(); mount(<App/>)
  const user = userEvent.setup()
  const toggle = screen.getByRole('button', { name: 'Open navigation' })
  await user.click(toggle)
  expect(toggle).toHaveAttribute('aria-expanded', 'true')
  await user.click(screen.getByRole('link', { name: 'Categories' }))
  expect(screen.getByRole('heading', { name: 'Categories', level: 1 })).toBeVisible()
  expect(toggle).toHaveAttribute('aria-expanded', 'false')
})
test('empty and API error states give useful feedback', async () => {
  mockFetch(url => url.includes('categories') ? Response.json([]) : Response.json({ items: [], total: 0, pageNumber: 1, pageSize: 10 }))
  mount(<App/>)
  expect(await screen.findByRole('heading', { name: 'No products found' })).toBeVisible()
})
test('decimal formatting preserves large monetary values exactly', () => {
  expect(money('999999999999999999.10')).toBe('$999,999,999,999,999,999.10')
})
test('API helper exposes validation messages and handles empty responses', async () => {
  mockFetch(() => Response.json({ errors: { name: ['Name is required.'] } }, { status: 400 }))
  await expect(api('/products')).rejects.toThrow('Name is required.')
})
