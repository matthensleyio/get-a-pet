param(
    [int]$IntervalSeconds = 60
)

$ErrorActionPreference = 'Continue'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$stateFile = Join-Path $scriptDir 'state.json'
$countUrl = 'https://petbridge.org/animals/animals-count-available.php?ClientID=2&Species=Dog'
$listUrl = 'https://petbridge.org/animals/animals-all-responsive.php?ClientID=2&Species=Dog'
$detailUrlTemplate = 'https://petbridge.org/animals/animals-detail.php?ID={0}&ClientID=2&Species=Dog'
$profileUrlTemplate = 'https://kshumane.org/adoption/pet-details/?aid={0}&cid=2&tid=Dog'

function Get-DogCount {
    $resp = Invoke-WebRequest -Uri $countUrl -UseBasicParsing -TimeoutSec 15
    if ($resp.Content -match '<span class="counter">(\d+)') {
        return [int]$Matches[1]
    }
    throw "Could not parse dog count from response"
}

function Get-AllDogs {
    $resp = Invoke-WebRequest -Uri $listUrl -UseBasicParsing -TimeoutSec 30
    $html = $resp.Content

    $dogs = @()
    $pattern = '(?s)<div class="animal_list_box[^"]*"[^>]*>.*?</div>\s*</div>\s*<!-- animal_list_box -->'
    $matches = [regex]::Matches($html, $pattern)

    foreach ($m in $matches) {
        $card = $m.Value

        $aid = $null
        if ($card -match 'aid=(\d+)') { $aid = $Matches[1] }

        $name = $null
        if ($card -match 'class="results_animal_link">([^<]+)</a>') { $name = $Matches[1] }

        $age = $null
        if ($card -match 'results_animal_detail_data_Age">([^<]+)<') { $age = $Matches[1] }

        $gender = $null
        if ($card -match 'results_animal_detail_data_Sex">([^<]+)<') { $gender = $Matches[1] }

        $photoUrl = $null
        if ($card -match 'class="results_animal_image"[^>]*src="([^"]+)"') {
            $photoUrl = $Matches[1]
        } elseif ($card -match 'src="([^"]+)"[^>]*class="results_animal_image"') {
            $photoUrl = $Matches[1]
        } elseif ($card -match '<img[^>]+src="(https://g\.petango\.com[^"]+)"') {
            $photoUrl = $Matches[1]
        }

        if ($aid) {
            $dogs += [PSCustomObject]@{
                Aid       = $aid
                Name      = $name
                Age       = $age
                Gender    = $gender
                PhotoUrl  = $photoUrl
                ProfileUrl = $profileUrlTemplate -f $aid
            }
        }
    }

    return $dogs
}

function Get-DogBreed {
    param([string]$Aid)

    $url = $detailUrlTemplate -f $Aid
    try {
        $resp = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 15
        if ($resp.Content -match 'Breed:</span>\s*([^<]+)') {
            return $Matches[1].Trim()
        }
    } catch {
        Write-Host "  Warning: could not fetch breed for AID $Aid - $($_.Exception.Message)"
    }
    return $null
}

function Send-DogNotification {
    param([PSCustomObject]$Dog, [string]$Breed)

    $tempPhoto = $null
    if ($Dog.PhotoUrl) {
        try {
            $ext = if ($Dog.PhotoUrl -match '\.(jpe?g|png|gif|webp)') { ".$($Matches[1])" } else { '.jpg' }
            $tempPhoto = Join-Path $env:TEMP "khs_dog_$($Dog.Aid)$ext"
            Invoke-WebRequest -Uri $Dog.PhotoUrl -OutFile $tempPhoto -UseBasicParsing -TimeoutSec 10
        } catch {
            Write-Host "  Warning: could not download photo for $($Dog.Name)"
            $tempPhoto = $null
        }
    }

    $line1 = "New Dog: $($Dog.Name)"
    $line2Parts = @()
    if ($Dog.Gender) { $line2Parts += $Dog.Gender }
    if ($Dog.Age) { $line2Parts += $Dog.Age }
    $line2 = $line2Parts -join ', '
    $line3 = if ($Breed) { "Breed: $Breed" } else { $null }

    $textLines = @($line1)
    if ($line2) { $textLines += $line2 }
    if ($line3) { $textLines += $line3 }

    $toastParams = @{
        Text = $textLines
        Button = New-BTButton -Content 'View Profile' -Arguments $Dog.ProfileUrl
    }
    if ($tempPhoto -and (Test-Path $tempPhoto)) {
        $toastParams.AppLogo = $tempPhoto
    }

    New-BurntToastNotification @toastParams

    if ($tempPhoto -and (Test-Path $tempPhoto)) {
        Start-Sleep -Milliseconds 500
        try { Remove-Item $tempPhoto -Force -ErrorAction SilentlyContinue } catch {}
    }
}

function Load-State {
    if (Test-Path $stateFile) {
        return Get-Content $stateFile -Raw | ConvertFrom-Json
    }
    return $null
}

function Save-State {
    param([int]$Count, [string[]]$KnownAids, [hashtable]$KnownDogs)

    @{
        count     = $Count
        knownAids = $KnownAids
        knownDogs = $KnownDogs
        updated   = (Get-Date -Format 'o')
    } | ConvertTo-Json | Set-Content $stateFile -Encoding UTF8
}

Import-Module BurntToast -ErrorAction Stop
Write-Host "get-a-pet started at $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host "Checking every $IntervalSeconds seconds (active 5am-8pm Central)"
Write-Host "Press Ctrl+C to stop"
Write-Host ""

while ($true) {
    try {
        $centralTz = [TimeZoneInfo]::FindSystemTimeZoneById('Central Standard Time')
        $centralNow = [TimeZoneInfo]::ConvertTimeFromUtc([DateTime]::UtcNow, $centralTz)
        $hour = $centralNow.Hour

        if ($hour -lt 5 -or $hour -ge 20) {
            Start-Sleep -Seconds $IntervalSeconds
            continue
        }

        $state = Load-State

        $currentCount = Get-DogCount

        if ($null -eq $state) {
            Write-Host "[$($centralNow.ToString('HH:mm:ss'))] Count: $currentCount - First run, capturing initial state..."
            $dogs = Get-AllDogs
            $aids = @($dogs | ForEach-Object { $_.Aid })
            $dogMap = @{}
            foreach ($d in $dogs) { $dogMap[$d.Aid] = $d.Name }
            Save-State -Count $currentCount -KnownAids $aids -KnownDogs $dogMap
            Write-Host "  Initial state captured: $($aids.Count) dogs saved to state.json"
            Start-Sleep -Seconds $IntervalSeconds
            continue
        }

        if ($currentCount -eq $state.count) {
            Start-Sleep -Seconds $IntervalSeconds
            continue
        }

        Write-Host "[$($centralNow.ToString('HH:mm:ss'))] Count: $currentCount (was $($state.count)), checking for changes..."
        $dogs = Get-AllDogs
        $currentAids = @($dogs | ForEach-Object { $_.Aid })
        $knownAids = @($state.knownAids)
        $newDogs = @($dogs | Where-Object { $_.Aid -notin $knownAids })
        $removedAids = @($knownAids | Where-Object { $_ -notin $currentAids })

        if ($removedAids.Count -gt 0) {
            $savedDogs = @{}
            if ($state.knownDogs) {
                $state.knownDogs.PSObject.Properties | ForEach-Object { $savedDogs[$_.Name] = $_.Value }
            }
            Write-Host "  $($removedAids.Count) dog(s) removed:"
            foreach ($aid in $removedAids) {
                $removedName = $savedDogs[$aid]
                if ($removedName) {
                    Write-Host "  <- $removedName (AID $aid)"
                } else {
                    Write-Host "  <- Unknown (AID $aid)"
                }
            }
        }

        if ($newDogs.Count -gt 0) {
            Write-Host "  Found $($newDogs.Count) new dog(s)!"
            foreach ($dog in $newDogs) {
                Write-Host "  -> $($dog.Name) ($($dog.Gender), $($dog.Age))"
                $breed = Get-DogBreed -Aid $dog.Aid
                if ($breed) { Write-Host "     Breed: $breed" }
                Send-DogNotification -Dog $dog -Breed $breed
            }
        }

        if ($newDogs.Count -eq 0 -and $removedAids.Count -eq 0) {
            Write-Host "  No dog changes detected (count mismatch may be transient)"
        }

        $dogMap = @{}
        foreach ($d in $dogs) { $dogMap[$d.Aid] = $d.Name }
        Save-State -Count $currentCount -KnownAids $currentAids -KnownDogs $dogMap
    } catch {
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Error: $($_.Exception.Message)"
    }

    Start-Sleep -Seconds $IntervalSeconds
}
