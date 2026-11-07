# Verification results

Verified on **7 November 2026**. Evidence records actual executed tests; no generated screenshot or inferred test pass is used.

| Check | Result | Evidence |
| --- | --- | --- |
| Backend build | Pass, Release | GitHub backend job |
| PostgreSQL/xUnit | **35 passed, 0 failed, 0 skipped** | `verification/backend.trx` and CI backend artifact |
| Frontend production build | Pass | GitHub frontend job; local Vite output |
| TypeScript / ESLint | Pass | GitHub frontend job |
| Vitest / RTL | **12 passed** | `verification/frontend.xml` and CI frontend artifact |
| Playwright | **3 passed** against native app and **3 passed** against Docker app | `verification/playwright.json`, `verification/playwright-docker.json`; CI Chromium and local Edge |
| Docker build/startup/smoke | **Pass locally on Windows and in GitHub Actions** | `verification/docker-smoke.json`, `verification/docker-status.json`; CI container artifact |
| Windows PowerShell 5.1 configuration | Pass | Generated 48-character secret in isolated local test directory |
| Dependency audit | No known npm/API package advisories at verification time | `verification/npm-audit.json`, `verification/nuget-audit.json` |
| Actual screenshots | Seven captured and inspected | `docs/screenshots/` |

Latest verified application-code PR workflow: [Validation run 42388234077](https://github.com/Jaclyn25/inventory-management-system/actions/runs/42388234077). Backend, frontend, Chromium, container and security checks passed. Its immutable head commit is recorded by GitHub; documentation delivery has its own subsequent workflow. The final merged-main run is linked in the completion report and remains available from [workflow history](https://github.com/Jaclyn25/inventory-management-system/actions/workflows/ci.yml).

Local Docker built both production images and started PostgreSQL, the API and Nginx successfully. All three containers were healthy. The smoke check passed through the production proxy on port 18081, including exact summary/category value reconciliation. Initial NuGet/image downloads were slow; the completed build and startup succeeded without deleting database data.

The local TRX and XML/JSON files provide individual case results and timestamps. Verbose `Output` nodes were removed from the committed TRX to keep the evidence compact; case identities, outcomes, counters and timings are preserved. CI artifacts retain the original logs for 14 days; committed evidence persists. Browser runs create fictional QA records. Exact screenshot totals reflect the capture database and need not match a later database containing QA records.

Testing covers the documented local application. It is not a formal accessibility certification, penetration test, accounting certification, load test or backup/recovery validation.
