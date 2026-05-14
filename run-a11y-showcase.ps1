# FreeAI/run-a11y-showcase.ps1
#
# Boots each FreeCRM-based project in InMemory mode, runs FreeA11yChecker
# Console against it, saves reports + screenshots to Docs/showcase/runs/.
#
# Usage:
#   cd FreeAI
#   .\run-a11y-showcase.ps1
#   .\run-a11y-showcase.ps1 -Targets FreeGLBA,FreeLLM
#   .\run-a11y-showcase.ps1 -SkipBuild

param(
    [string[]]$Targets = @(),
    [switch]$SkipBuild
)

$ErrorActionPreference = "Continue"
$root = $PSScriptRoot

$allTargets = @(
    @{ Name="ChatWithAI";       Project="ChatWithAI\FreeAI\FreeAI.csproj";                                           Port=5100; ShowcaseRel="ChatWithAI\Docs\showcase" },
    @{ Name="FreeGLBA";         Project="FreeGLBA\FreeGLBA\FreeGLBA.csproj";                                         Port=5101; ShowcaseRel="FreeGLBA\Docs\showcase" },
    @{ Name="FreeLLM";          Project="FreeLLM\FreeLLM\FreeLLM.csproj";                                            Port=5102; ShowcaseRel="FreeLLM\Docs\showcase" },
    @{ Name="FreeManager";      Project="FreeManager\FreeManager\FreeManager.csproj";                                 Port=5103; ShowcaseRel="FreeManager\Docs\showcase" },
    @{ Name="FreePlugins";      Project="FreePlugins\FreePluginsV1\FreePlugins\FreePlugins.csproj";                   Port=5104; ShowcaseRel="FreePlugins\Docs\showcase" },
    @{ Name="FreeServicesHub";  Project="FreeServicesHub\FreeServicesHub\FreeServicesHub\FreeServicesHub.csproj";     Port=5105; ShowcaseRel="FreeServicesHub\Docs\showcase" },
    @{ Name="FreeSmartsheets";  Project="FreeSmartsheets\FreeSmartsheets\FreeSmartsheets\FreeSmartsheets.csproj";     Port=5106; ShowcaseRel="FreeSmartsheets\Docs\showcase" },
    @{ Name="FreeBlazorExample";Project="FreeBlazorExtended\FreeBlazorExample\FreeBlazorExample\FreeBlazorExample.csproj"; Port=5107; ShowcaseRel="FreeBlazorExtended\Docs\showcase" }
)

$consoleDll = "$root\FreeA11yChecker\.publish\console\FreeA11yChecker.Console.dll"

if ($Targets.Count -gt 0) {
    $allTargets = $allTargets | Where-Object { $Targets -contains $_.Name }
}

function Stop-Tree([int]$Id) {
    try {
        $children = Get-CimInstance Win32_Process -Filter "ParentProcessId=$Id" -ErrorAction SilentlyContinue
        foreach ($c in $children) { Stop-Tree $c.ProcessId }
        Stop-Process -Id $Id -Force -ErrorAction SilentlyContinue
    } catch {}
}

function Wait-ForUrl([string]$Url, [int]$TimeoutSec = 90) {
    for ($i = 0; $i -lt $TimeoutSec; $i++) {
        try {
            $req = [System.Net.WebRequest]::Create($Url)
            $req.Timeout = 2000
            $req.AllowAutoRedirect = $false
            $null = $req.GetResponse()
            return $true
        } catch [System.Net.WebException] {
            # Got a response (even 302/4xx) = server is up
            if ($_.Exception.Response -ne $null) { return $true }
        } catch { }
        Start-Sleep -Seconds 1
    }
    return $false
}

# Build scanner once
if (-not $SkipBuild) {
    $consoleCsproj = "$root\FreeA11yChecker\FreeA11yChecker.Console\FreeA11yChecker.Console.csproj"
    $out = "$root\FreeA11yChecker\.publish\console"
    Write-Host "Building scanner..." -ForegroundColor Cyan
    dotnet publish $consoleCsproj -c Release -o $out --nologo -v quiet 2>&1 | Where-Object { $_ -match "error" } | Select-Object -Last 5
    if ($LASTEXITCODE -ne 0) { Write-Error "Scanner build failed"; exit 1 }
}

if (-not (Test-Path $consoleDll)) { Write-Error "Scanner DLL not found at $consoleDll"; exit 1 }

$results = @()

foreach ($t in $allTargets) {
    $projectPath = "$root\$($t.Project)"
    $showcaseDir = "$root\$($t.ShowcaseRel)"
    $runDate     = Get-Date -Format "yyyy-MM-dd"
    $runDir      = "$showcaseDir\runs\$runDate"
    $url         = "http://localhost:$($t.Port)"

    New-Item -ItemType Directory -Path $runDir -Force | Out-Null

    Write-Host "`n━━━ $($t.Name) → $url ━━━" -ForegroundColor Cyan

    if (-not $SkipBuild) {
        Write-Host "  Building..." -NoNewline
        dotnet build $projectPath --nologo -v quiet 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Host " FAILED" -ForegroundColor Red
            $results += [pscustomobject]@{ Name=$t.Name; Status="BuildFailed" }
            continue
        }
        Write-Host " OK" -ForegroundColor Green
    }

    # Start app — no stdout redirect to avoid pipe deadlock
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $proc = Start-Process dotnet -ArgumentList @(
        "run","--project",$projectPath,"--no-build","--no-launch-profile",
        "--","--urls",$url
    ) -PassThru -WindowStyle Hidden

    Write-Host "  Waiting for $url (pid $($proc.Id))..." -NoNewline
    $ready = Wait-ForUrl $url 90

    if (-not $ready) {
        Write-Host " TIMEOUT" -ForegroundColor Red
        Stop-Tree $proc.Id
        $results += [pscustomobject]@{ Name=$t.Name; Status="Timeout" }
        continue
    }
    Write-Host " OK ($(($proc.TotalProcessorTime.TotalSeconds).ToString('F1'))s CPU)" -ForegroundColor Green

    # Run FreeA11yChecker Console
    Write-Host "  Scanning..."
    $scanLog = & dotnet $consoleDll scan `
        --url $url `
        --user admin `
        --pass admin `
        "--Scanner:OutputDir=$runDir" 2>&1
    $scanLog | Out-File "$runDir\scan-log.txt"

    $screenshotCount = (Get-ChildItem $runDir -Filter "*.png" -Recurse -ErrorAction SilentlyContinue).Count
    Write-Host "  Scan done — $screenshotCount screenshots, exit $LASTEXITCODE"

    Stop-Tree $proc.Id
    Start-Sleep -Seconds 2

    $status = if ($LASTEXITCODE -eq 0) { "OK" } else { "ScanError($LASTEXITCODE)" }
    $results += [pscustomobject]@{ Name=$t.Name; Status=$status; Screenshots=$screenshotCount; Output=$runDir }
}

Write-Host "`n━━━ Summary ━━━" -ForegroundColor Cyan
$results | Format-Table -AutoSize