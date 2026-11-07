# Actual application screenshots

Captured with Playwright from the running Stockroom app and fictional seed data. These are browser screenshots, not generated mockups. The capture script waits for data to finish loading and supports a configurable local base URL.

| Screen | File |
| --- | --- |
| Dashboard | [dashboard.png](../docs/screenshots/dashboard.png) |
| Product catalog | [products.png](../docs/screenshots/products.png) |
| Categories | [categories.png](../docs/screenshots/categories.png) |
| Stock movement history | [movements.png](../docs/screenshots/movements.png) |
| Valuation reports | [reports.png](../docs/screenshots/reports.png) |
| Stock operation dialog | [stock-operation.png](../docs/screenshots/stock-operation.png) |
| Mobile dashboard, 390px viewport | [mobile.png](../docs/screenshots/mobile.png) |

Desktop captures use a 1440px viewport; full-page capture can be taller than the visible window. Screenshots were inspected for loaded content, hierarchy and responsive layout. To reproduce, follow [setup](../docs/setup.md) and run `node frontend/scripts/preview.mjs` with a seeded local app.
