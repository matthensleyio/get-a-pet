$ErrorActionPreference = 'Stop'

Write-Host "Starting get-a-pet (local dev)..." -ForegroundColor Cyan

$azuriteDataDir = "$PSScriptRoot/.azurite"
if (-not (Test-Path $azuriteDataDir)) {
    New-Item -ItemType Directory -Path $azuriteDataDir | Out-Null
}

$azuriteJob = Start-Job -ScriptBlock {
    azurite --silent --location "$using:azuriteDataDir"
}

Start-Sleep -Seconds 2

$apiJob = Start-Job -ScriptBlock {
    Set-Location "$using:PSScriptRoot/api"
    func start
}

Start-Sleep -Seconds 5

$frontendJob = Start-Job -ScriptBlock {
    Set-Location "$using:PSScriptRoot/ui"
    npm run dev
}

$monitorJob = Start-Job -ScriptBlock {
    Set-Location "$using:PSScriptRoot/monitor"
    dotnet run
}

Write-Host ""
Write-Host "Azurite:  127.0.0.1:10000-10002" -ForegroundColor Green
Write-Host "API:      http://localhost:7071" -ForegroundColor Green
Write-Host "Frontend: http://localhost:5173" -ForegroundColor Green
Write-Host "Monitor:  running (scrapes every 60s, 05:00-20:00 Central)" -ForegroundColor Green
Write-Host ""
Write-Host "Press Ctrl+C to stop all servers" -ForegroundColor Yellow

try {
    while ($true) {
        Start-Sleep -Seconds 1

        if ($azuriteJob.State -eq 'Failed') {
            Write-Host "Azurite failed:" -ForegroundColor Red
            Receive-Job $azuriteJob
            break
        }

        if ($apiJob.State -eq 'Failed') {
            Write-Host "API server failed:" -ForegroundColor Red
            Receive-Job $apiJob
            break
        }

        if ($frontendJob.State -eq 'Failed') {
            Write-Host "Frontend server failed:" -ForegroundColor Red
            Receive-Job $frontendJob
            break
        }

        if ($monitorJob.State -eq 'Failed') {
            Write-Host "Monitor failed:" -ForegroundColor Red
            Receive-Job $monitorJob
            break
        }
    }
}
finally {
    Write-Host "`nStopping servers..." -ForegroundColor Yellow
    Stop-Job $azuriteJob -ErrorAction SilentlyContinue
    Stop-Job $apiJob -ErrorAction SilentlyContinue
    Stop-Job $frontendJob -ErrorAction SilentlyContinue
    Stop-Job $monitorJob -ErrorAction SilentlyContinue
    Remove-Job $azuriteJob -Force -ErrorAction SilentlyContinue
    Remove-Job $apiJob -Force -ErrorAction SilentlyContinue
    Remove-Job $frontendJob -Force -ErrorAction SilentlyContinue
    Remove-Job $monitorJob -Force -ErrorAction SilentlyContinue
    Write-Host "Done." -ForegroundColor Green
}
