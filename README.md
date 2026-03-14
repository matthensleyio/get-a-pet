# get-a-pet

A PWA + Azure Functions app that monitors multiple KC-area shelters (via petbridge.org) for newly listed dogs and sends web push notifications.

## What It Does

- Scrapes KHS, KC Pet Project, and Great Plains SPCA listings on a schedule
- Diffs the current dog list against the previously stored list
- Sends browser push notifications for newly listed dogs (filtered by shelter preference)
- Shows a dashboard of all currently listed dogs with photos and details
- Tracks recently adopted dogs

## Solution Structure

```
GetAPet.sln
  app/core                   - Shared library (domain models, engines, repositories, orchestrators)
  app/api                    - Azure Functions API (read/subscribe endpoints only)
  app/GetAPet.Shelter.Import - Dockerized monitor worker
```

## Prerequisites

- [.NET SDK 9](https://dotnet.microsoft.com/download)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite) (local Azure Storage emulator)
- Node.js
- VAPID keys (generate with `npx web-push generate-vapid-keys`)

## Local Setup

1. Create `app/api/local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "VAPID_SUBJECT": "mailto:you@example.com",
    "VAPID_PUBLIC_KEY": "...",
    "VAPID_PRIVATE_KEY": "..."
  }
}
```

2. Start Azurite.

3. Start the API:

```bash
cd app/api && func start
```

4. Start the frontend:

```bash
cd app/ui && npm run dev
```

- API: http://localhost:7071
- Frontend: http://localhost:5173

## Commands

### Build API

```bash
cd app/api && dotnet build
```

### Build Frontend

```bash
cd app/ui && npm run build
```

## Architecture

### Request Flow (API)

```
GET  /api/status          -> StatusFunction -> StatusOrchestrator
GET  /api/vapid-public-key -> VapidFunction
POST /api/subscribe       -> SubscribeFunction
DELETE /api/subscribe     -> SubscribeFunction
GET  /api/share/{aid}     -> OgFunction (OG preview HTML + redirect)
```

### Monitor Flow

The monitor runs as a Dockerized worker (`app/GetAPet.Shelter.Import`), not via HTTP trigger.

```
MonitorWorker (PeriodicTimer, 5 min)
  -> time-guard: skip outside 5am-8pm Central
  -> MonitorOrchestrator.CheckAsync()
    -> AdoptedDogRepository.PruneOldAsync()
    -> ScrapingEngine.GetAllDogsAsync()      [all 3 shelters]
    -> DogDiffEngine.ComputeDiff()
    -> ScrapingEngine.GetDogDetailAsync()   [per dog, parallel]
    -> NotificationEngine.SendAsync()       [new dogs, filtered by subscriber shelter prefs]
    -> AdoptedDogRepository.SaveAsync()     [removed dogs archived]
    -> DogRepository.RemoveDogsAsync()
    -> DogRepository.UpsertDogsAsync()
    -> StateRepository.SaveStateAsync()
```

### Layer Boundaries

- **Functions** (`app/api/Functions/`) - HTTP trigger handlers; map DTOs, no business logic
- **Orchestrators** - coordinate engines and repositories
- **Engines** (`app/core/Engines/`) - pure business logic (scraping, diff, notifications)
- **Repositories** (`app/core/Repositories/`) - Azure Table Storage access
- **DomainModels** (`app/core/DomainModels/`) - `record` types used across all layers
- **Dtos** (`app/api/Dtos/`) - serialized request/response shapes at the Function boundary only

### Frontend

Vite + React + TypeScript PWA (`app/ui/`). Key structure:
- `app/ui/src/` - React source (components, pages, hooks, context, utils, types)
- `app/ui/public/` - Static assets (sw.js, manifest.json, icons, staticwebapp.config.json)
- Routes: `/` (home, dog grid + tabs) and `/dogs/:aid/details` (dog detail page)
- TanStack Query polls `/api/status` every 30s; idb for offline cache
- `sw.js` handles push events and `notificationclick` (navigates to `/dogs/:aid/details`)

### Shelters

| ID | Name | Petbridge CID |
|----|------|---------------|
| khs | KHS (Humane Society of Greater KC) | 2 |
| kcpp | KC Pet Project | 11 |
| gpspca | Great Plains SPCA | 17 |

### Storage Schema

All tables use `AzureWebJobsStorage`:

| Table | PartitionKey | RowKey |
|-------|-------------|--------|
| `Dogs` | `"dog"` | Aid |
| `SiteState` | `"state"` | `"latest"` |
| `PushSubscriptions` | `"sub"` | SHA256(endpoint) hex |
| `AdoptedDogs` | `"adopted"` | Aid |

Tables are created at startup before the host starts.

## Key Configuration

`app/api/local.settings.json` and `app/monitor/appsettings.json` (gitignored) must contain:

- `AzureWebJobsStorage` / `STORAGE_CONNECTION_STRING` - Table Storage connection string
- `VAPID_SUBJECT` - mailto: address
- `VAPID_PUBLIC_KEY`
- `VAPID_PRIVATE_KEY`

## Deployment

**API + Frontend**: Azure Static Web Apps. Push to `main` triggers the SWA pipeline, which builds from `app/ui/` and deploys `dist/` alongside `app/api/` as the Functions backend. Configure VAPID keys as SWA Application Settings.

**Monitor**: Deployed as a separate container via `app/GetAPet.Shelter.Import/docker-compose.yml`. Requires `STORAGE_CONNECTION_STRING` and VAPID keys in an `.env` file alongside the compose file.

```bash
cd app/GetAPet.Shelter.Import && docker compose up -d
```
