# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

**get-a-pet** — A PWA + Azure Functions app that monitors multiple Kansas City shelters for newly listed dogs and sends web push notifications. It replaces `Monitor-Dogs.ps1`.

Monitored shelters:
- Kansas Humane Society (KHS) — via PetBridge
- KC Pet Project — via PetBridge
- Great Plains SPCA — via PetBridge
- Pawsitive Tails Dog Rescue — via ShelterLuv API
- Humane Society of Greater Kansas City — via ShelterLuv V3 API

## Commands

### Run Locally

```powershell
.\app\start-local.ps1
```

This starts both servers in parallel:
- API: `cd app/api && func start` -> http://localhost:7071
- Frontend: `cd app/ui && npm run dev` -> http://localhost:5173

Prerequisites: Azure Functions Core Tools v4, Azurite (local storage emulator) running, VAPID keys populated in `app/api/local.settings.json`.

### Build API

```bash
cd app/api && dotnet build
```

### Build Frontend

```bash
cd app/ui && npm run build
```

## Architecture

### Monitor Flow

The monitor is not exposed as an HTTP endpoint. `MonitorOrchestrator.CheckAsync()` must be invoked externally (e.g., via Azure Logic App or a scheduled runner):

```
MonitorOrchestrator.CheckAsync()
  -> AdoptedDogRepository.PruneOldAsync() [prune >7 day records]
  -> StateRepository.GetStateAsync()
  -> ScrapingEngine.GetAllDogsAsync() [PetBridge shelters]
  -> ShelterLuvScrapingEngine.GetAllDogsAsync() [ShelterLuv shelters]
  -> ShelterLuvV3ScrapingEngine.GetAllDogsAsync() [ShelterLuv V3 shelters]
  -> merge + deduplicate by composite key ("{shelterId}-{aid}")
  -> [first run] DogRepository.UpsertDogsAsync() + StateRepository.SaveStateAsync() [no notifications]
  -> [subsequent] DogDiffEngine.ComputeDiff()
    -> [new dogs] ScrapingEngine.GetDogDetailAsync() [per PetBridge dog, parallel]
    -> NotificationEngine.SendAsync() [per notifiable dog, per subscriber]
    -> [removed dogs] AdoptedDogRepository.SaveAsync() + DogRepository.RemoveDogsAsync()
    -> DogRepository.UpsertDogsAsync()
    -> StateRepository.SaveStateAsync()
```

Every monitor invocation does a full scrape and replaces the stored dog list. ShelterLuv dogs already include full detail; only PetBridge dogs require a separate detail fetch. Notifications are suppressed for first-time shelters and for dogs notified within the past 7 days.

### HTTP API Endpoints

- `GET /api/status` — Returns current dogs and recently adopted dogs (polls every 30s from frontend)
- `GET /api/shelters` — Returns list of all configured shelters
- `GET /api/vapid-public-key` — Returns VAPID public key for push subscription setup
- `POST /api/subscribe` — Register push notification subscription
- `DELETE /api/subscribe` — Unregister push notification subscription
- `GET /api/share/{aid}` — OpenGraph preview HTML for dog sharing (redirects to detail page)

### Layer Boundaries

- **Functions** (`app/api/Functions/`) - HTTP trigger handlers; map DTOs, no business logic
- **Orchestrators** (`app/api/Orchestrators/`) - coordinate engines/repositories for HTTP responses (e.g., `StatusOrchestrator`)
- **Orchestrators** (`app/core/Orchestrators/`) - coordinate engines/repositories for the monitor flow (`MonitorOrchestrator`)
- **Engines** (`app/core/Engines/`) - pure business logic (scraping, diff, notifications)
- **Repositories** (`app/core/Repositories/`) - Azure Table Storage access; map `TableEntity` to/from domain models internally
- **DomainModels** (`app/core/DomainModels/`) - `record` types used across all layers
- **Dtos** (`app/api/Dtos/`) - serialized request/response shapes at the Function boundary only

No interfaces; single implementations. Primary constructors on all classes. `TreatWarningsAsErrors` is enabled.

### Frontend

Vite + React + TypeScript PWA (`app/ui/`). Builds to `app/ui/dist/`. Key structure:
- `app/ui/src/` - React source (components, pages, hooks, context, utils, types)
- `app/ui/public/` - Static assets (sw.js, manifest.json, icons, staticwebapp.config.json)
- `app/ui/src/App.css` - Global CSS (no CSS modules)
- Routes: `/` (home, dog grid + tabs) and `/dogs/:aid/details` (dog detail page)
- TanStack Query polls `/api/status` every 30s; idb for offline cache
- `sw.js` handles push events and `notificationclick` (navigates to `/dogs/:aid/details`)

### Storage Schema

All four tables use `AzureWebJobsStorage`:
- `Dogs` - PartitionKey=`"dog"`, RowKey=composite key (`"{shelterId}-{aid}"`)
- `SiteState` - PartitionKey=`"state"`, RowKey=`"latest"`
- `PushSubscriptions` - PartitionKey=`"sub"`, RowKey=SHA256(endpoint) as hex
- `AdoptedDogs` - PartitionKey=`"adopted"`, RowKey=composite key; pruned after 7 days

Tables are created at startup in `Program.cs` before the host starts.

## Key Configuration

`app/api/local.settings.json` (gitignored) must contain:

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

In Azure: these are SWA Application Settings. `AzureWebJobsStorage` is auto-provided by the Functions runtime.

## Deployment

Azure Static Web Apps. `app/ui/public/staticwebapp.config.json` configures routing, security headers, and `apiRuntime: dotnet-isolated:9.0`. The SWA build pipeline builds from `app/ui/` (runs `npm install && npm run build`) and deploys `dist/` as the frontend alongside `app/api/` as the Functions API.

The monitor has no HTTP trigger and no timer trigger in the deployed function — it must be driven externally (e.g., an Azure Logic App calling `MonitorOrchestrator.CheckAsync()` on a schedule).
