import { chromium } from '@playwright/test'
import { mkdir } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
const output = fileURLToPath(new URL('../../docs/screenshots/', import.meta.url))
const browser = await chromium.launch({ channel: process.env.PLAYWRIGHT_CHANNEL || undefined })
const page = await browser.newPage({ viewport: { width: 1440, height: 1000 }, deviceScaleFactor: 1 })
const base = process.env.CAPTURE_BASE_URL || 'http://127.0.0.1:5174'
await mkdir(output, { recursive: true })
for (const [route, name, text] of [
  ['/', 'dashboard', 'Replenishment watchlist'],
  ['/products', 'products', 'Adjustable desk riser'],
  ['/categories', 'categories', 'Desk & stationery'],
  ['/movements', 'movements', 'Fictional workplace dispatch'],
  ['/reports', 'reports', 'Current-cost inventory value'],
]) {
  await page.goto(base + route)
  await page.getByText(text, { exact: true }).first().waitFor()
  await page.getByRole('status', { name: 'Loading' }).waitFor({ state: 'hidden' })
  await page.screenshot({ path: `${output}/${name}.png`, fullPage: true, animations: 'disabled' })
}
await page.goto(base + '/products')
await page.getByRole('button', { name: 'Move stock for Adjustable desk riser', exact: true }).click()
await page.getByLabel('Movement type').waitFor()
await page.screenshot({ path: `${output}/stock-operation.png`, fullPage: true })
await page.getByRole('button', { name: 'Cancel', exact: true }).click()
await page.setViewportSize({ width: 390, height: 844 })
await page.goto(base + '/')
await page.getByText('Replenishment watchlist', { exact: true }).waitFor()
await page.screenshot({ path: `${output}/mobile.png`, fullPage: true, animations: 'disabled' })
await browser.close()
