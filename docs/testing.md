# Test strategy and verification

## Backend: real PostgreSQL

35 xUnit tests exercise HTTP endpoints through WebApplicationFactory and the real Npgsql provider. The PostgreSQL fixture creates a unique database, applies all migrations, and drops it afterward. It fails rather than silently skipping when `INVENTORY_TEST_CONNECTION` is absent. No SQLite or in-memory replacement is used.

Coverage includes product validation/create/update/archive, duplicate SKU normalization, category uniqueness/references, receipts, withdrawals, negative stock, duplicate business operations and keys, exact replay, quantity limits, immutable history, adjustments/reasons/stale counts, UTC timestamps, exact valuation, stock health, dashboard totals, queries, API errors and seed reconciliation.

Concurrency tests send 20 parallel increases, 20 competing decreases, ten same-key retries, competing adjustments and duplicate operation IDs across different products. The oversell test requires exactly five withdrawals to succeed against ten units when each asks for two, 15 conflicts, final zero stock and a reconciled ledger. Reporting is read while 20 receipts execute; every response must agree with its category/unit/value totals.

```powershell
pwsh ./scripts/init-env.ps1
docker compose up -d db
pwsh ./scripts/test.ps1 --logger 'trx;LogFileName=backend.trx' --results-directory TestResults
```

On another OS, set `INVENTORY_TEST_CONNECTION` to an admin connection with CREATEDB and run `dotnet test -c Release`. Tests touch only their uniquely named databases. The fictional seed test creates a second temporary database to verify repeatability and complete stock/ledger reconciliation.

## Frontend interactions

12 Vitest/React Testing Library tests cover catalog status/search/filter requests, required fields, exact decimal text payloads, no quantity in catalog writes, conflict feedback, insufficient-stock validation, safe same-identity retry after network failure, mobile navigation state, empty states, API validation messages, large decimal formatting, dashboard values/chart alternatives and report filters.

```powershell
cd frontend
npm ci
npm run typecheck
npm run build
npm run lint
npm test -- --reporter=default --reporter=junit --outputFile.junit=test-results/frontend.xml
npm audit
```

On Windows, use `npm.cmd` if the installed PowerShell npm shim drops forwarded arguments. The evidence can also be generated directly with `node node_modules/vitest/vitest.mjs run --reporter=default --reporter=junit --outputFile.junit=test-results/frontend.xml` from `frontend`.

The minimatch override selects a patched release across the dev lint graph. Build/lint/tests run with that graph; do not remove it without reassessing the advisory. Lockfiles record the actual dependency resolution.

## Browser flows

Playwright uses real API/database data and creates uniquely named fictional QA products/categories:

1. Create a product through the UI, receive ten units and verify quantity/status.
2. Withdraw four of ten units and verify six remaining, exact `$0.60` cost value and a `-4` ledger entry.
3. At 390×844, open/navigate/close the mobile sidebar, inspect the product form bounds and check report/history page overflow.

```powershell
cd frontend
npx playwright install chromium
npm run test:e2e
```

Run the native API first; Playwright starts Vite automatically unless `E2E_BASE_URL` is set. For Docker, set that URL to `http://127.0.0.1:18081`. On Windows, `PLAYWRIGHT_CHANNEL=msedge` selects installed Edge. Browser QA records persist in the selected development database, so use a disposable database for clean demos.

## Docker and CI

```powershell
docker compose up --build --detach --wait --wait-timeout 180
node scripts/smoke.mjs
```

The smoke check verifies the web shell, database readiness, reports, exact summary/category value reconciliation, OpenAPI and security headers through Nginx. CI has independent backend, frontend, browser and container jobs. All jobs must pass before merging. Jobs retain TRX, JUnit, Playwright and container logs/status artifacts for 14 days. Native browser CI uses Chromium and disposable PostgreSQL; container CI independently builds and starts the production images.

See [actual results and evidence](../submission/TEST-RESULTS.md). Evidence is historical: run the commands and inspect the workflow linked to the current commit for a fresh verification. Automated tests and manual screenshot inspection do not establish formal WCAG certification or production security readiness.
