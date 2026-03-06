# KHS Dog Monitor

A PWA + Azure Functions app that monitors [kshumane.org](https://kshumane.org) (via petbridge.org) for newly listed dogs and sends web push notifications.

## What It Does

- Scrapes the KHS Petbridge listing on a schedule
- Diffs the current dog list against the previously stored list
- Sends browser push notifications for newly listed dogs
- Shows a dashboard of all currently listed dogs with photos and details

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite) (local Azure Storage emulator)
- Node.js (for `npx serve`)
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

2. Start Azurite (local storage emulator).

3. Run both servers:

```powershell
.\app\start-local.ps1
```

- API: http://localhost:7071
- Frontend: http://localhost:3000

## Commands

### Build API

```bash
cd app/api && dotnet build
```

### Trigger Monitor Manually

```bash
curl -X POST http://localhost:7071/api/monitor
```

## Architecture

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

**Backend:** .NET 9 Azure Functions (isolated worker), Azure Table Storage

**Frontend:** Vanilla JS PWA (`app/src/`), no build step. Polls `/api/status`, manages push subscriptions, caches to IndexedDB for offline use.

## Deployment

Hosted on Azure Static Web Apps. Push to `main` triggers the SWA build pipeline, which serves the frontend from `app/src/` and the API from `app/api/`.

The monitor is triggered client-side on a schedule (the frontend polls and fires `POST /api/monitor`). There is no Azure timer trigger.

In Azure, configure these Application Settings on the SWA:

- `VAPID_SUBJECT`
- `VAPID_PUBLIC_KEY`
- `VAPID_PRIVATE_KEY`

`AzureWebJobsStorage` is auto-provided by the Functions runtime.
