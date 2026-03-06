# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

**get-a-pet** — A PWA + Azure Functions app that monitors kshumane.org (via petbridge.org) for newly listed dogs and sends web push notifications. It replaces `Monitor-Dogs.ps1`.

## Commands

### Run Locally

```powershell
.\app\start-local.ps1
```

This starts both servers in parallel:
- API: `cd app/api && func start` -> http://localhost:7071
- Frontend: `npx serve app/src -l 3000` -> http://localhost:3000

Prerequisites: Azure Functions Core Tools v4, Azurite (local storage emulator) running, VAPID keys populated in `app/api/local.settings.json`.

### Build API

```bash
cd app/api && dotnet build
```

### Trigger Monitor Manually (local)

```bash
curl -X POST http://localhost:7071/api/monitor
```

## Architecture

### Request Flow

```
HTTP POST /api/monitor
  -> MonitorHttpFunction (time-guard: skip outside 5am-8pm Central)
    -> MonitorOrchestrator.CheckAsync()
      -> ScrapingEngine.GetAllDogsAsync()
      -> DogDiffEngine.ComputeDiff()
      -> [new dogs] ScrapingEngine.GetDogBreedAsync() [per dog, parallel]
      -> NotificationEngine.SendAsync() [per dog, per subscriber]
      -> DogRepository.RemoveDogsAsync() [removed dogs]
      -> DogRepository.UpsertDogsAsync()
      -> StateRepository.SaveStateAsync()
```

Every monitor invocation does a full scrape and replaces the stored dog list.

### Layer Boundaries

- **Functions** (`app/api/Functions/`) - HTTP trigger handlers; map DTOs, no business logic
- **Orchestrators** (`app/api/Orchestrators/`) - coordinate engines and repositories
- **Engines** (`app/api/Engines/`) - pure business logic (scraping, diff, notifications)
- **Repositories** (`app/api/Repositories/`) - Azure Table Storage access; map `TableEntity` to/from domain models internally
- **DomainModels** (`app/api/DomainModels/`) - `record` types used across all layers
- **Dtos** (`app/api/Dtos/`) - serialized request/response shapes at the Function boundary only

No interfaces; single implementations. Primary constructors on all classes. `TreatWarningsAsErrors` is enabled.

### Frontend

Vanilla JS PWA (`app/src/`). No build step. `app.js` polls `/api/status`, manages push subscriptions, and caches to IndexedDB for offline. `sw.js` handles push events and `notificationclick`.

### Storage Schema

All three tables use `AzureWebJobsStorage`:
- `Dogs` - PartitionKey=`"dog"`, RowKey=Aid (petbridge animal ID)
- `SiteState` - PartitionKey=`"state"`, RowKey=`"latest"`
- `PushSubscriptions` - PartitionKey=`"sub"`, RowKey=SHA256(endpoint) as hex

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

Azure Static Web Apps. `app/src/staticwebapp.config.json` configures routing, security headers, and `apiRuntime: dotnet-isolated:9.0`. The SWA build pipeline serves the frontend from `app/src/` and the API from `app/api/`.

The monitor trigger (`POST /api/monitor`) is client-driven—it must be called externally on a schedule (e.g., Azure Logic App or browser-side polling) since there is no timer trigger in the deployed function.
