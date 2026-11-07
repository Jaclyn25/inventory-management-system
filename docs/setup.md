# Setup guide

## Windows 11 and Docker Desktop

1. Install Git and Docker Desktop. Enable Linux containers/WSL2 and start the engine.
2. Clone the existing repository, using an authorized GitHub account if access is restricted.
3. From the repository root, run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\init-env.ps1
docker compose up --build --detach --wait --wait-timeout 180
docker compose ps
```

Open [http://127.0.0.1:18081](http://127.0.0.1:18081). Windows PowerShell 5.1 configuration generation was actually exercised. The script writes ASCII configuration and creates a 48-character random hexadecimal secret; it never overwrites an existing `.env`.

Initial builds download images and packages. Health checks wait after building. If a network download fails, rerun the Compose command; completed layers are cached. Do not erase the database to fix a network or build failure.

## Configuration

| Variable | Purpose |
| --- | --- |
| `POSTGRES_DB`, `POSTGRES_USER` | Database identity, defaults shown in `.env.example` |
| `POSTGRES_PASSWORD` | Required generated local secret; never commit `.env` |
| `INVENTORY_WEB_PORT` | Optional loopback web port, default 18081 |
| `INVENTORY_SEED` | Compose fictional seed toggle, default true |
| `ConnectionStrings__Inventory` | Explicit native connection string; takes precedence over separate DB variables |
| `Database__Host`, `Database__Port` | API host/port when composing a connection from variables |
| `Database__Migrate`, `Database__Seed` | Apply migrations / seed an empty catalog at API startup |
| `INVENTORY_TEST_CONNECTION` | PostgreSQL admin connection with CREATEDB for disposable integration databases |
| `PLAYWRIGHT_CHANNEL` | Optional installed browser channel, such as msedge |
| `E2E_BASE_URL` | Use an existing app for browser tests instead of starting Vite |
| `CAPTURE_BASE_URL` | Running app used by screenshot capture script |

`.env.example` deliberately contains no password. On Linux/macOS, copy it to `.env` and supply a strong random value. Keep the generated hexadecimal format when using the native helper scripts.

If Windows reserves 18081, add `INVENTORY_WEB_PORT=18082` to `.env`, rerun Compose and open that port. PostgreSQL uses loopback 54330. The API has no published host port. `docker compose logs --tail 100 api web` helps diagnose readiness; do not share `.env` or rendered Compose configuration containing secrets.

`docker compose stop` preserves containers/data. `docker compose down` preserves the named database volume. Removing the volume intentionally destroys data; there is no automatic backup or restore feature.

## Native development

Requires PowerShell 7 for the `pwsh` commands shown, .NET SDK 10 and Node 22.13+ or 24 LTS. The Docker path does not need these host runtimes. The SDK policy allows installed .NET 10 feature bands; JavaScript uses the committed lockfile.

```powershell
pwsh ./scripts/init-env.ps1
docker compose up -d db
dotnet tool restore
dotnet restore
pwsh ./scripts/start-api.ps1
```

In a second terminal:

```powershell
cd frontend
npm ci
npm run dev
```

Vite: [127.0.0.1:5174](http://127.0.0.1:5174). API: [127.0.0.1:5081](http://127.0.0.1:5081/api/health). To run without helper scripts, supply a connection string plus `Database__Migrate=true` and optionally `Database__Seed=true`, then use `dotnet run --project backend/Stockroom.Api --no-launch-profile --urls http://127.0.0.1:5081`.

To add a migration:

```powershell
dotnet tool restore
dotnet ef migrations add YourChange --project backend/Stockroom.Api
```

To apply migrations separately, set `ConnectionStrings__Inventory` and run `dotnet ef database update --project backend/Stockroom.Api`. Startup migrations are appropriate for this local single-API setup; production needs controlled migration permissions and orchestration.

## Screenshots and source packaging

```powershell
$env:PLAYWRIGHT_CHANNEL='msedge' # optional on Windows
$env:CAPTURE_BASE_URL='http://127.0.0.1:18081'
node frontend/scripts/preview.mjs
pwsh ./scripts/package-source.ps1
```

Capture expects the fictional seed records and writes actual browser PNGs under `docs/screenshots`. Source packaging archives committed HEAD and excludes `.env`, dependencies and build outputs. Review the submission checklist manually; do not upload automatically.
