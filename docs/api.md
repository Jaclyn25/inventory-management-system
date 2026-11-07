# API guide

Base URL: `/api`. OpenAPI JSON: `/openapi/v1.json`. No authentication is implemented; use only in a trusted local environment. Request and response field names use camelCase.

A [captured OpenAPI schema](openapi.json) is included for offline review; the running endpoint describes the current application.

## Endpoints

| Method | Route | Behavior |
| --- | --- | --- |
| GET | `/api/health` | Database readiness |
| GET, POST | `/api/categories` | List / create category |
| PUT, DELETE | `/api/categories/{id}` | Rename / delete unreferenced category |
| GET, POST | `/api/products` | Query / create product |
| GET, PUT | `/api/products/{id}` | Detail / version-checked edit |
| POST | `/api/products/{id}/archive` | Archive current-version, active, zero-stock product |
| POST | `/api/stock/in` | Receive positive whole units |
| POST | `/api/stock/out` | Issue positive whole units without overselling |
| POST | `/api/stock/adjustment` | Set reviewed counted quantity; record the signed difference |
| GET | `/api/stock/movements` | Immutable movement history |
| GET | `/api/reports` | Snapshot summary, values, categories, alerts, recent movements and 14-day activity |

Successful creation returns `201`, reads/stock mutations return `200`, and archive/delete returns `204`. There are no product deletion, ledger editing or public deployment endpoints.

## Product payload

```json
{
  "sku": "NS-DEMO-01",
  "name": "Fictional desk notebook",
  "description": "Internship demonstration product",
  "categoryId": "11111111-1111-1111-1111-111111111111",
  "costPrice": "4.50",
  "unitPrice": "7.20",
  "lowStockThreshold": 10,
  "version": 1
}
```

Use a real category UUID returned by this app. SKU allows 2–40 ASCII letters/digits/hyphens/underscores, begins with a letter/digit and is normalized uppercase. Names are trimmed and required (maximum 160); description maximum 2,000. Prices allow at most two decimals and range 0–999,999,999.99. Quantity and thresholds are whole units with an upper limit of 1,000,000,000. New products always start at zero; catalog payloads do not change quantity. Edits require the version from a fresh product response.

Responses return prices and `inventoryValue` as decimal strings. Product detail includes current quantity, category name, version, archive state and UTC creation timestamp. `inventoryValue` is calculated on the server.

Archive body: `{ "version": 3 }`. Archive preserves SKU uniqueness and all history. Category input: `{ "name": "Fictional supplies" }`; names are unique after trimming/case normalization. Categories referenced by archived products also remain protected.

## Stock writes and retries

Every stock POST requires `Idempotency-Key: <nonempty UUID>` and a body such as:

```json
{
  "operationId": "22222222-2222-2222-2222-222222222222",
  "productId": "33333333-3333-3333-3333-333333333333",
  "quantity": 5,
  "reason": "Fictional delivery reference DEMO-001"
}
```

Generate fresh IDs for a new operation. Keep the same key **and** operation ID when retrying after a timeout. The same canonical request replays its original result without changing stock twice. A changed request under the key returns `409`. The operation ID is independently globally unique: repeating it with another key returns `409` and rolls back any stock change.

For `/stock/adjustment`, quantity is the **actual counted quantity** (zero is valid), and include `version` from the product that was reviewed. A stale version returns `409`; an unchanged count returns `400`. In/out quantities must be greater than zero. All operations require a trimmed reason of 3–500 characters and reject archived products.

Movement responses contain operation `id`, product ID, historical SKU/name/cost, kind, signed delta, before/after balances, reason and UTC timestamp. Retries return those historical balances, not the product's latest balance. No route can edit a movement. Correct mistakes by recording a reasoned adjustment.

## Query parameters

Products and reports: `search` (name/SKU/description, up to 160 characters), `categoryId`, `status=all|healthy|low|out`, `archived=false|true`. Low means positive quantity at or below threshold; out means zero. Healthy means above threshold. Product lists additionally accept `sort=name|sku|quantity|value`, `descending`, `page` (1–100000) and `pageSize` (1–100, default 20). Ties use product UUID for stable pagination. Reports apply filters to all summary components; pagination/sort parameters do not limit report totals.

Movement history: `productId`, `search` (historical SKU/name/reason), `kind=in|out|adjustment`, `from`, `to`, `page`, `pageSize`. Use ISO timestamps with an offset; filters are converted to UTC and bounds are inclusive. From must not exceed To. History is ordered newest first, then UUID.

Paged responses: `{ "items": [], "total": 0, "pageNumber": 1, "pageSize": 20 }`. Reports return total products/units, cost and retail values, separate low/out counts, category aggregates, up to eight attention products/recent movements, 14 daily activity rows, currency and snapshot timestamp.

## Errors

Errors use `application/problem+json`: `400` for invalid inputs, precision, count limits or no-op adjustments; `404` for missing resources/routes; `405` for unsupported methods; `409` for duplicates, stale versions, references, archived mutations and insufficient stock. DTO validation problems include an `errors` field. Unexpected failures return a sanitized `500` problem and trace ID; structured server logs retain diagnostics without credentials.

Clients must refresh after a stale-count/metadata conflict, preserve retry identity after uncertain delivery and never retry a stock request under a new identity merely because the response was lost.
