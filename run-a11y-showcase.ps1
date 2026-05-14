# FreeAI/run-a11y-showcase.ps1
#
# Boots each FreeCRM-based project in InMemory mode, crawls every page
# (seeds from .razor @page routes + link discovery), runs FreeA11yChecker
# Console, and saves reports + screenshots to Docs/showcase/runs/latest/.
# Each run overwrites the previous latest/ — git history shows the diff.
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

# SourcePath = folder crawl seeds @page routes from (relative to $root)
$allTargets = @(
    @{ Name="ChatWithAI";       Project="ChatWithAI\FreeAI\FreeAI.csproj";                                                Port=5100; ShowcaseRel="ChatWithAI\Docs\showcase";       SourcePath="ChatWithAI" },
    @{ Name="FreeGLBA";         Project="FreeGLBA\FreeGLBA\FreeGLBA.csproj";                                              Port=5101; ShowcaseRel="FreeGLBA\Docs\showcase";         SourcePath="FreeGLBA" },
    @{ Name="FreeLLM";          Project="FreeLLM\FreeLLM\FreeLLM.csproj";                                                 Port=5102; ShowcaseRel="FreeLLM\Docs\showcase";          SourcePath="FreeLLM" },
    @{ Name="FreeManager";      Project="FreeManager\FreeManager\FreeManager.csproj";                                     Port=5103; ShowcaseRel="FreeManager\Docs\showcase";      SourcePath="FreeManager" },
    @{ Name="FreePlugins";      Project="FreePlugins\FreePluginsV1\FreePlugins\FreePlugins.csproj";                       Port=5104; ShowcaseRel="FreePlugins\Docs\showcase";      SourcePath="FreePlugins" },
    @{ Name="FreeServicesHub";  Project="FreeServicesHub\FreeServicesHub\FreeServicesHub\FreeServicesHub.csproj";         Port=5105; ShowcaseRel="FreeServicesHub\Docs\showcase";  SourcePath="FreeServicesHub" },
    @{ Name="FreeSmartsheets";  Project="FreeSmartsheets\FreeSmartsheets\FreeSmartsheets\FreeSmartsheets.csproj";         Port=5106; ShowcaseRel="FreeSmartsheets\Docs\showcase";  SourcePath="FreeSmartsheets" },
    @{ Name="FreeBlazorExample";Project="FreeBlazorExtended\FreeBlazorExample\FreeBlazorExample\FreeBlazorExample.csproj";Port=5107; ShowcaseRel="FreeBlazorExtended\Docs\showcase";SourcePath="FreeBlazorExtended" },
    @{ Name="FreeA11yChecker";  Project="FreeA11yChecker\FreeA11yChecker\FreeA11yChecker.csproj";                        Port=5108; ShowcaseRel="FreeA11yChecker\Docs\showcase";  SourcePath="FreeA11yChecker" }
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
            if ($_.Exception.Response -ne $null) { return $true }
        } catch { }
        Start-Sleep -Seconds 1
    }
    return $false
}

# Build scanner once (picks up RunLogger.cs change to fixed scan.log name)
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
    $runDir      = "$showcaseDir\runs\latest"
    $sourcePath  = "$root\$($t.SourcePath)"
    $url         = "http://localhost:$($t.Port)"

    # Wipe latest/ before each run so old pages don't linger — git history tracks diffs
    if (Test-Path $runDir) {
        Remove-Item $runDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $runDir -Force | Out-Null

    Write-Host "`n--- $($t.Name) -> $url ---" -ForegroundColor Cyan

    if (-not $SkipBuild) {
        Write-Host "  Building..." -NoNewline
        dotnet build $projectPath --nologo -v quiet 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Host " FAILED" -ForegroundColor Red
            $results += [pscustomobject]@{ Name=$t.Name; Status="BuildFailed"; Screenshots=0; Output=$runDir }
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
        $results += [pscustomobject]@{ Name=$t.Name; Status="Timeout"; Screenshots=0; Output=$runDir }
        continue
    }
    $cpuSec = "{0:F1}" -f $proc.TotalProcessorTime.TotalSeconds
    Write-Host " OK (${cpuSec}s CPU)" -ForegroundColor Green

    # Crawl all pages: seeds from .razor @page directives + follows discovered links
    Write-Host "  Crawling all pages..."
    $scanLog = & dotnet $consoleDll crawl `
        --url $url `
        --user admin `
        --pass admin `
        --source-path $sourcePath `
        "--Scanner:OutputDir=$runDir" 2>&1
    $scanLog | Out-File "$runDir\scan-log.txt" -Encoding UTF8

    $screenshotCount = (Get-ChildItem $runDir -Include "*.png","*.jpeg" -Recurse -ErrorAction SilentlyContinue).Count
    Write-Host "  Crawl done — $screenshotCount screenshots, exit $LASTEXITCODE"

    Stop-Tree $proc.Id
    Start-Sleep -Seconds 2

    $status = if ($LASTEXITCODE -eq 0) { "OK" } else { "ScanError($LASTEXITCODE)" }
    $results += [pscustomobject]@{ Name=$t.Name; Status=$status; Screenshots=$screenshotCount; Output=$runDir }
}

Write-Host "`n--- Summary ---" -ForegroundColor Cyan
$results | Format-Table -AutoSize
