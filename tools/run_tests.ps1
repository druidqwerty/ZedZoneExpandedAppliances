$ErrorActionPreference = "Stop"

$projDir = "F:\DeepSeek Harness workflows\MODS\projects\zedzone-expanded-appliances"
$gameDir = "G:\Steam\steamapps\common\ZED ZONE"
$normScript = "C:\Users\druid\.openclaw\workspace-modder\projects\zedzone-smart-companions\tools\normalize_assembly_refs.ps1"
$modInstallDir = Join-Path $gameDir "Mods\ExpandedAppliances"
$testInstallDir = Join-Path $gameDir "Mods\ZExpandedAppliancesVerify"
$realSaveRoot = "C:\Users\druid\AppData\LocalLow\LevenLiu\ZEDZONE"
$labRoot = Join-Path $env:TEMP ("zz-ea-live-lab-" + (Get-Date -Format "yyyyMMdd-HHmmss"))

Write-Host "=== 1. Build ExpandedAppliances + normalize AssemblyRef 0.0.0.0 ==="
dotnet build "$projDir\ExpandedAppliances.csproj" -c Release
if ($LASTEXITCODE -ne 0) { throw "ExpandedAppliances build failed!" }
& $normScript -DllPath "$projDir\bin\ExpandedAppliances.dll"

Write-Host "=== 2. Build TestHarness ==="
dotnet build "$projDir\tools\TestHarness\ZExpandedAppliancesVerify.csproj" -c Release
if ($LASTEXITCODE -ne 0) { throw "TestHarness build failed!" }

Write-Host "=== 3. Install candidate mod + temporary test harness ==="
New-Item -ItemType Directory -Force -Path $modInstallDir | Out-Null
Copy-Item "$projDir\bin\ExpandedAppliances.dll" "$modInstallDir\ExpandedAppliances.dll" -Force
Copy-Item "$projDir\mod.json" "$modInstallDir\mod.json" -Force

New-Item -ItemType Directory -Force -Path $testInstallDir | Out-Null
Copy-Item "$projDir\tools\TestHarness\bin\ZExpandedAppliancesVerify.dll" "$testInstallDir\ZExpandedAppliancesVerify.dll" -Force
Copy-Item "$projDir\tools\TestHarness\mod.json" "$testInstallDir\mod.json" -Force

$appIdFile = Join-Path $gameDir "steam_appid.txt"
$createdAppId = -not (Test-Path $appIdFile)
if ($createdAppId) { "1211600" | Set-Content $appIdFile -NoNewline -Encoding Ascii }

$reportHeadless = "$projDir\tools\TestHarness\verify_report.txt"
$reportLive = "$projDir\tools\TestHarness\live_verify_report.txt"
if (Test-Path $reportHeadless) { Remove-Item $reportHeadless -Force }
if (Test-Path $reportLive) { Remove-Item $reportLive -Force }

function Invoke-ZedRun([string]$arguments, [string]$expectedFile, [int]$timeoutSec) {
    $logOut = Join-Path $gameDir "BepInEx\LogOutput.log"
    if (Test-Path $logOut) { Remove-Item $logOut -Force -ErrorAction SilentlyContinue }
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = Join-Path $gameDir "ZEDZONE.exe"
    $psi.Arguments = $arguments
    $psi.WorkingDirectory = $gameDir
    $psi.UseShellExecute = $false
    $psi.EnvironmentVariables["SteamAppId"] = "1211600"
    $psi.EnvironmentVariables["SteamGameId"] = "1211600"
    $proc = [System.Diagnostics.Process]::Start($psi)
    $sw = [Diagnostics.Stopwatch]::StartNew()
    try {
        while ($sw.Elapsed.TotalSeconds -lt $timeoutSec) {
            if ((Test-Path $expectedFile) -or $proc.HasExited) { break }
            Start-Sleep -Milliseconds 300
        }
        Start-Sleep -Milliseconds 1000
        if (-not $proc.HasExited) { $proc.Kill() }
        Write-Host "Run finished in $($sw.Elapsed.TotalSeconds.ToString('0.0'))s (report=$(Test-Path $expectedFile), exited=$($proc.HasExited))"
    } finally {
        if ($proc -and -not $proc.HasExited) { try { $proc.Kill() } catch {} }
    }
}

try {
    Write-Host "=== 4. Phase 1: Headless Suite ==="
    Invoke-ZedRun "-batchmode -nographics" $reportHeadless 60

    Write-Host "=== 5. Prepare isolated live-world save lab ==="
    New-Item -ItemType Directory -Force -Path $labRoot | Out-Null
    Copy-Item (Join-Path $realSaveRoot "GameData") (Join-Path $labRoot "GameData") -Recurse -Force
    Write-Host "Lab prepared at: $labRoot"

    Write-Host "=== 6. Phase 2: Graphical Live-World Suite (Gaming + Real BatteryCharger + LootInventoryPanel PNG) ==="
    Invoke-ZedRun "-screen-fullscreen 0 -screen-width 1920 -screen-height 1080 --ea-live-lab=$labRoot" $reportLive 120
} finally {
    Start-Sleep -Milliseconds 2000
    for ($i = 0; $i -lt 5; $i++) {
        if (Test-Path $testInstallDir) {
            try { Remove-Item $testInstallDir -Recurse -Force -ErrorAction Stop; break } catch { Start-Sleep -Milliseconds 800 }
        }
    }
    if ($createdAppId -and (Test-Path $appIdFile)) { Remove-Item $appIdFile -Force }
    if (Test-Path $labRoot) { Remove-Item $labRoot -Recurse -Force -ErrorAction SilentlyContinue }
}

Write-Host "=== 7. Headless Report ==="
if (Test-Path $reportHeadless) { Get-Content $reportHeadless } else { Write-Error "Headless report missing!" }

Write-Host "=== 8. Live-World Report ==="
if (Test-Path $reportLive) { Get-Content $reportLive } else { Write-Error "Live-world report missing!" }

Write-Host "=== 9. BepInEx Log excerpt (ExpandedAppliances / Verify / Errors) ==="
Select-String -Path "$gameDir\BepInEx\LogOutput.log" -Pattern "ExpandedAppliances|ZExpandedAppliancesVerify|Exception|Error|Failed" -CaseSensitive:$false | Select-Object -First 45 | ForEach-Object { $_.Line }
