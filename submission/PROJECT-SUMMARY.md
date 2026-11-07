# Stockroom · Internship Task 3

**Applicant repository:** [Jaclyn25/inventory-management-system](https://github.com/Jaclyn25/inventory-management-system). GitHub reports public visibility at final verification, despite the task describing it as private. Visibility was not changed.

Stockroom is an independent inventory application for small-business stock coordinators. It combines a searchable product catalog, category management, transaction-safe stock operations, immutable movement history, current-cost valuation and an accessible responsive dashboard.

## Mandatory feature checklist

- [x] Create, view, edit and archive products; unique SKU; categories; descriptions; exact prices; thresholds; current quantities; search/filter/sort/pagination.
- [x] Stock in, stock out, counted adjustments, reasons, timestamps and searchable/filterable history.
- [x] No negative stock, product-row serialization, atomic balance/ledger commits, globally unique operation IDs, idempotency keys and immutable ledger trigger.
- [x] Version-checked edits/adjustments and restrictive category/archive rules.
- [x] Exact current-cost and retail valuation; consistent report snapshots under concurrent writes.
- [x] Inventory summary, low/out-of-stock reporting, category distribution and stock activity.
- [x] Responsive dashboard, navigation, forms/dialogs, confirmation flows, stock indicators and empty/loading/error states.
- [x] Real PostgreSQL xUnit integration tests, frontend interaction tests and three critical Playwright flows.
- [x] Docker Compose and GitHub Actions for builds, types, lint, tests, browser flows and container startup smoke checks.
- [x] Architecture/API/setup/testing documentation, actual screenshots, Git delivery record and verification evidence.

## Deliverables

| Item | Location |
| --- | --- |
| Source | Repository root; generate `submission/stockroom-source.zip` with `scripts/package-source.ps1` |
| Application overview | [Root README](../README.md) |
| Architecture | [architecture.md](../docs/architecture.md) |
| API guide | [api.md](../docs/api.md) and running `/openapi/v1.json` |
| Windows/Docker setup | [setup.md](../docs/setup.md) |
| Test coverage | [testing.md](../docs/testing.md) |
| Actual screenshots | [Screenshot index](SCREENSHOTS.md), `docs/screenshots/` |
| Verification | [TEST-RESULTS.md](TEST-RESULTS.md), `submission/verification/` and GitHub artifacts |
| Git/PR delivery | [DELIVERY.md](DELIVERY.md) |

## Engineering choices

ASP.NET Core controllers delegate to scoped business services, with EF Core migrations and PostgreSQL constraints. Stock operations lock a product inside a transaction, preserve retry identity and write an immutable movement. Decimal text prevents browser money arithmetic. Reports use repeatable-read snapshots. React uses TanStack Query, React Hook Form, Zod and Recharts.

The completed library project was reviewed for engineering/delivery quality. Stockroom has its own product, domain, ledger semantics, design and tests. The existing inventory repository was used throughout; no other internship repository or visibility setting was changed.

## Submission preparation

Review the source ZIP, summary, screenshots, setup and evidence before manually uploading to the internship portal. If a private repository link is required, arrange reviewer access yourself without changing visibility. Optional video recording is not included; a useful demo order is dashboard, product creation, receipt, withdrawal, adjustment, reports and mobile navigation.

**No public deployment or Auspify submission has been performed.** There is no authentication; run only in a trusted local environment. See README limitations before assessing deployment suitability.
