# A11yAudit/run-audit.ps1
# CI / pre-build convenience wrapper around the A11yAudit console app.
# For interactive use, just set A11yAudit as the startup project and press F5,
# or: dotnet run --project A11yAudit [args]
#
# This script is only useful when you need to build the solution first
# (e.g. a clean CI machine) and want to pass filter flags from a shell.
#
# Usage:
#   .\A11yAudit\run-audit.ps1                    # build + scan everything
#   .\A11yAudit\run-audit.ps1 -SkipBuild         # skip dotnet build, run immediately
#   .\A11yAudit\run-audit.ps1 -LocalOnly         # BlazorApp1 + FreeA11yChecker only
#   .\A11yAudit\run-audit.ps1 -ExternalOnly      # WSU sites only
#   .\A11yAudit\run-audit.ps1 -Target BlazorApp1 # single target by name

param(
	[switch]$SkipBuild,
	[string]$Target = "",
	[switch]$LocalOnly,
	[switch]$ExternalOnly
)

$ErrorActionPreference = "Stop"
$AuditDir   = $PSScriptRoot
$RepoRoot   = Split-Path $AuditDir -Parent
$SolutionFile = Join-Path $RepoRoot "FreeA11yChecker.slnx"

function Write-Step($msg) { Write-Host "`n=== $msg ===" -ForegroundColor Cyan }

if (-not $SkipBuild) {
	Write-Step "Building solution"
	if (Test-Path $SolutionFile) {
		dotnet build $SolutionFile --nologo -v q
	} else {
		dotnet build "$AuditDir\A11yAudit.csproj" --nologo -v q
	}
	if ($LASTEXITCODE -ne 0) { throw "Build failed" }
}

Write-Step "Running A11yAudit"
$auditArgs = @()
if ($Target)       { $auditArgs += $Target }
if ($LocalOnly)    { $auditArgs += "--local-only" }
if ($ExternalOnly) { $auditArgs += "--external-only" }

Push-Location $AuditDir
try {
	dotnet run --no-build -- @auditArgs
	exit $LASTEXITCODE
} finally {
	Pop-Location
}
