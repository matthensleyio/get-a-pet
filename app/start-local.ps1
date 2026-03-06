$ErrorActionPreference = 'Stop'

Write-Host "Starting KHS Dog Monitor (local dev)..." -ForegroundColor Cyan

$apiJob = Start-Job -ScriptBlock {
    Set-Location "$using:PSScriptRoot/api"
    func start
}

Start-Sleep -Seconds 5

$frontendJob = Start-Job -ScriptBlock {
    Set-Location "$using:PSScriptRoot/src"
    npx serve . -l 3000
}

Write-Host ""
Write-Host "API:      http://localhost:7071" -ForegroundColor Green
Write-Host "Frontend: http://localhost:3000" -ForegroundColor Green
Write-Host ""
Write-Host "Press Ctrl+C to stop both servers" -ForegroundColor Yellow

try {
    while ($true) {
        Start-Sleep -Seconds 1

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
    }
}
finally {
    Write-Host "`nStopping servers..." -ForegroundColor Yellow
    Stop-Job $apiJob -ErrorAction SilentlyContinue
    Stop-Job $frontendJob -ErrorAction SilentlyContinue
    Remove-Job $apiJob -Force -ErrorAction SilentlyContinue
    Remove-Job $frontendJob -Force -ErrorAction SilentlyContinue
    Write-Host "Done." -ForegroundColor Green
}
