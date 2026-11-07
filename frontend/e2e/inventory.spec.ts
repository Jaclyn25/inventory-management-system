import { test, expect } from '@playwright/test'
import { randomUUID } from 'node:crypto'

test('product creation and stock increase', async ({ page }) => {
  const suffix = randomUUID().slice(0, 8)
  const name = `QA notebook ${suffix}`,
    sku = `E2E-${suffix}`
  await page.goto('/products')
  await page.getByRole('button', { name: 'Add product', exact: true }).click()
  const dialog = page.getByRole('dialog', { name: 'Add product' })
  await dialog.getByLabel('Product name').fill(name)
  await dialog.getByLabel('SKU', { exact: true }).fill(sku)
  await dialog.getByLabel('Description').fill('Fictional browser test product')
  await expect(dialog.getByLabel('Category').locator('option')).not.toHaveCount(1)
  await dialog.getByLabel('Category').selectOption({ index: 1 })
  await dialog.getByLabel('Cost price (USD)').fill('2.50')
  await dialog.getByLabel('Unit price (USD)').fill('4.75')
  await dialog.getByLabel('Low-stock threshold').fill('3')
  await dialog.getByRole('button', { name: 'Save product' }).click()
  await expect(dialog).not.toBeVisible()
  await page.getByRole('textbox', { name: 'Search products' }).fill(sku)
  const row = page.getByRole('row').filter({ hasText: sku })
  await expect(row).toHaveCount(1)
  await row.getByRole('button', { name: `Move stock for ${name}` }).click()
  const stock = page.getByRole('dialog', { name: 'Record stock movement' })
  await stock.getByLabel('Quantity', { exact: true }).fill('10')
  await stock.getByLabel('Reason').fill(`Opening receipt ${suffix}`)
  await stock.getByRole('button', { name: 'Confirm movement' }).click()
  await expect(stock).not.toBeVisible()
  await expect(row.getByText('10', { exact: true })).toBeVisible()
  await expect(row.getByText('In stock', { exact: true })).toBeVisible()
  await expect(page.getByRole('status')).toContainText('Stock in recorded.')
})

test('stock decrease and exact report verification', async ({ page, request }) => {
  const suffix = randomUUID().slice(0, 8),
    sku = `E2E-OUT-${suffix}`,
    name = `QA dispatch ${suffix}`
  const categoryResponse = await request.post('/api/categories', { data: { name: `QA reports ${suffix}` } })
  expect(categoryResponse.status()).toBe(201)
  const category = await categoryResponse.json()
  const response = await request.post('/api/products', {
    data: {
      sku,
      name,
      description: 'Fictional dispatch test',
      categoryId: category.id,
      costPrice: '0.10',
      unitPrice: '0.30',
      lowStockThreshold: 2,
    },
  })
  expect(response.status()).toBe(201)
  const product = await response.json()
  const receipt = await request.post('/api/stock/in', {
    headers: { 'Idempotency-Key': randomUUID() },
    data: {
      operationId: randomUUID(),
      productId: product.id,
      quantity: 10,
      reason: 'Fictional test receipt',
    },
  })
  expect(receipt.ok()).toBeTruthy()
  await page.goto('/products')
  await page.getByRole('textbox', { name: 'Search products' }).fill(sku)
  await page.getByRole('button', { name: `Move stock for ${name}` }).click()
  const dialog = page.getByRole('dialog', { name: 'Record stock movement' })
  await dialog.getByLabel('Movement type').selectOption('out')
  await dialog.getByLabel('Quantity', { exact: true }).fill('4')
  await dialog.getByLabel('Reason').fill(`Customer dispatch ${suffix}`)
  await dialog.getByRole('button', { name: 'Confirm movement' }).click()
  await expect(dialog).not.toBeVisible()
  await page.getByRole('link', { name: 'Reports', exact: true }).click()
  await page.getByRole('textbox', { name: 'Search report' }).fill(sku)
  await expect(page.getByTestId('report-value')).toHaveText('$0.60')
  const row = page.getByRole('row').filter({ hasText: sku })
  await expect(row.getByRole('cell', { name: '6', exact: true })).toBeVisible()
  await expect(row.getByRole('cell', { name: '$0.60', exact: true })).toBeVisible()
  await row.getByRole('link', { name: `History for ${name}` }).click()
  await expect(page.getByRole('cell', { name: `Customer dispatch ${suffix}`, exact: true })).toBeVisible()
  await expect(page.getByRole('cell', { name: '-4', exact: true })).toBeVisible()
})

test('mobile navigation and responsive product form', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Inventory overview' })).toBeVisible()
  const toggle = page.getByRole('button', { name: 'Open navigation' })
  await expect(toggle).toBeVisible()
  await expect(page.getByRole('navigation', { name: 'Primary navigation' })).not.toBeInViewport()
  await toggle.click()
  await expect(toggle).toHaveAttribute('aria-expanded', 'true')
  await page.getByRole('link', { name: 'Products', exact: true }).click()
  await expect(toggle).toHaveAttribute('aria-expanded', 'false')
  await expect(page.getByRole('heading', { name: /Products/, level: 1 })).toBeVisible()
  await page.getByRole('button', { name: 'Add product', exact: true }).click()
  const dialog = page.getByRole('dialog', { name: 'Add product' })
  await expect(dialog).toBeVisible()
  const bounds = await dialog.boundingBox()
  expect(bounds!.x).toBeGreaterThanOrEqual(0)
  expect(bounds!.width).toBeLessThanOrEqual(390)
  await dialog.getByRole('button', { name: 'Cancel' }).click()
  await toggle.click()
  await page.getByRole('link', { name: 'Reports', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Inventory reports' })).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1)).toBe(true)
  await toggle.click()
  await page.getByRole('link', { name: 'Stock movements', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Stock movements' })).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1)).toBe(true)
})
