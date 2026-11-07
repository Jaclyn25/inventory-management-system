import { chromium } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
const browser = await chromium.launch({ channel: process.env.PLAYWRIGHT_CHANNEL || 'msedge' });
const page = await browser.newPage({ viewport: { width: 1440, height: 1000 }, deviceScaleFactor: 1 });
await page.goto('http://127.0.0.1:5174/products');
await page.getByRole('button', { name: 'Adjustable desk riser', exact: true }).waitFor();
await mkdir('../docs/screenshots', { recursive: true });
await page.screenshot({ path: '../docs/screenshots/products.png', fullPage: true });
await browser.close();
