# A11yAudit/run-audit.ps1
# Orchestrates accessibility scans against BlazorApp1 and FreeA11yChecker,
# copying curated results into the A11yAudit evidence folder for commit.
#
# Usage:  .\A11yAudit\run-audit.ps1            (from repo root)
#         .\A11yAudit\run-audit.ps1 -SkipBuild  (skip dotnet build)
#         .\A11yAudit\run-audit.ps1 -Target BlazorApp1   (scan only one target)
#         .\A11yAudit\run-audit.ps1 -Target FreeA11yChecker

param(
	[switch]$SkipBuild,
	[ValidateSet("All", "BlazorApp1", "FreeA11yChecker")]
	[string]$Target = "All"
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path $PSScriptRoot -Parent
if (-not (Test-Path "$RepoRoot\FreeA11yChecker.slnx")) {
	$RepoRoot = $PSScriptRoot  # running from repo root directly
}

$AuditDir   = Join-Path $RepoRoot "A11yAudit"
$ConsoleDir = Join-Path $RepoRoot "FreeA11yChecker.Console"

# ── Helpers ──

function Write-Step($msg) { Write-Host "`n=== $msg ===" -ForegroundColor Cyan }
function Write-Ok($msg)   { Write-Host "  ✓ $msg" -ForegroundColor Green }
function Write-Warn($msg) { Write-Host "  ! $msg" -ForegroundColor Yellow }

function Wait-ForUrl([string]$Url, [int]$TimeoutSeconds = 60) {
	$end = (Get-Date).AddSeconds($TimeoutSeconds)
	while ((Get-Date) -lt $end) {
		try {
			# Ignore SSL errors for localhost dev certs
			$null = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5 -SkipCertificateCheck
			return $true
		} catch { Start-Sleep -Seconds 2 }
	}
	return $false
}

function Stop-BackgroundApp($process, $name) {
	if ($process -and -not $process.HasExited) {
		Write-Host "  Stopping $name (PID $($process.Id))..."
		try { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue } catch {}
		Start-Sleep -Seconds 2
	}
}

function Copy-ScanResults([string]$SourceDir, [string]$DestDir) {
	if (Test-Path $DestDir) { Remove-Item $DestDir -Recurse -Force }
	if (Test-Path $SourceDir) {
		Copy-Item $SourceDir $DestDir -Recurse -Force
		$count = (Get-ChildItem $DestDir -Recurse -File | Measure-Object).Count
		$sizeMB = [math]::Round((Get-ChildItem $DestDir -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB, 1)
		Write-Ok "Copied $count files ($sizeMB MB) to $DestDir"
	} else {
		Write-Warn "No scan results found at $SourceDir"
	}
}

# ── Build ──

if (-not $SkipBuild) {
	Write-Step "Building solution"
	dotnet build "$RepoRoot\FreeA11yChecker.slnx" -c Release --nologo -v q
	if ($LASTEXITCODE -ne 0) { throw "Build failed" }
	Write-Ok "Build succeeded"
}

# ── Scan BlazorApp1 ──

if ($Target -eq "All" -or $Target -eq "BlazorApp1") {
	Write-Step "Scanning BlazorApp1"

	$blazorProj = Join-Path $RepoRoot "BlazorApp1\BlazorApp1\BlazorApp1.csproj"
	$blazorUrl  = "https://localhost:7046"

	Write-Host "  Starting BlazorApp1..."
	$blazorProc = Start-Process -FilePath "dotnet" `
		-ArgumentList "run --project `"$blazorProj`" --no-build -c Release" `
		-PassThru -WindowStyle Hidden

	if (-not (Wait-ForUrl $blazorUrl)) {
		Stop-BackgroundApp $blazorProc "BlazorApp1"
		throw "BlazorApp1 did not start within 60s at $blazorUrl"
	}
	Write-Ok "BlazorApp1 is running at $blazorUrl"

	Write-Host "  Running scanner..."
	Push-Location $ConsoleDir
	try {
		dotnet run --no-build -c Release -- scan `
			--url $blazorUrl `
			--pages "/,/counter,/weather,/auth,/Account/Login,/Account/Register,/Account/Manage,/Account/Manage/Email,/Account/Manage/ChangePassword,/Account/Manage/TwoFactorAuthentication,/Account/Manage/PersonalData,/Account/ForgotPassword,/Account/ResendEmailConfirmation,/Account/AccessDenied,/Account/Lockout" `
			--user "admin@example.com" --pass "Admin1234!" `
			--login-url "$blazorUrl/Account/Login" `
			--username-selector "input[id='Input.Email']" `
			--password-selector "input[id='Input.Password']" `
			--submit-selector "button[type='submit']"
	} finally {
		Pop-Location
	}

	Stop-BackgroundApp $blazorProc "BlazorApp1"

	# Copy results
	$runOutput = Join-Path $ConsoleDir "bin\Release\net10.0\runs\latest\localhost"
	if (-not (Test-Path $runOutput)) {
		$runOutput = Get-ChildItem (Join-Path $ConsoleDir "bin\Release\net10.0\runs\latest") -Directory | Select-Object -First 1 -ExpandProperty FullName
	}
	Copy-ScanResults $runOutput (Join-Path $AuditDir "BlazorApp1")
}

# ── Scan FreeA11yChecker (self-dogfood) ──

if ($Target -eq "All" -or $Target -eq "FreeA11yChecker") {
	Write-Step "Scanning FreeA11yChecker (self-dogfood)"

	$fcProj = Join-Path $RepoRoot "FreeA11yChecker\FreeA11yChecker.csproj"
	$fcUrl  = "https://localhost:7271"

	Write-Host "  Starting FreeA11yChecker with InMemory database..."
	$env:ASPNETCORE_ENVIRONMENT = "Audit"
	$fcProc = Start-Process -FilePath "dotnet" `
		-ArgumentList "run --project `"$fcProj`" --no-build -c Release" `
		-PassThru -WindowStyle Hidden `
		-Environment @{ ASPNETCORE_ENVIRONMENT = "Audit" }

	# FreeA11yChecker WASM is heavier — give it more time
	if (-not (Wait-ForUrl $fcUrl 90)) {
		Stop-BackgroundApp $fcProc "FreeA11yChecker"
		throw "FreeA11yChecker did not start within 90s at $fcUrl"
	}
	Write-Ok "FreeA11yChecker is running at $fcUrl"

	# Scannable pages (no parameterized routes — those need real data IDs)
	$fcPages = @(
		"/",
		"/Login",
		"/About",
		"/Compliance",
		"/Compliance/Rules",
		"/Compliance/Scorecard",
		"/Compliance/Search",
		"/Compliance/Tree",
		"/Scans",
		"/Settings",
		"/Settings/Sites",
		"/Settings/Users",
		"/Settings/UserGroups",
		"/Settings/Departments",
		"/Settings/DepartmentGroups",
		"/Settings/Tags",
		"/Settings/Suppressions",
		"/Settings/DeletedRecords",
		"/Settings/Files",
		"/Settings/Language",
		"/Settings/UDF",
		"/Settings/Tenants",
		"/Settings/AppSettings",
		"/Settings/ScanMonitor",
		"/Settings/AddUser",
		"/Settings/AddUserGroup",
		"/Settings/AddDepartment",
		"/Settings/AddDepartmentGroup",
		"/Settings/AddTag",
		"/Settings/AddTenant",
		"/Reports/Trends",
		"/Profile",
		"/ChangePassword",
		"/Setup",
		"/not-found"
	) -join ","

	Write-Host "  Running scanner..."
	Push-Location $ConsoleDir
	try {
		dotnet run --no-build -c Release -- scan `
			--url $fcUrl `
			--pages $fcPages `
			--user "admin@freea11ychecker.local" --pass "Admin1234!" `
			--auth-type "FreeCRM"
	} finally {
		Pop-Location
	}

	Stop-BackgroundApp $fcProc "FreeA11yChecker"
	Remove-Item Env:\ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue

	# Copy results
	$runOutput = Join-Path $ConsoleDir "bin\Release\net10.0\runs\latest\localhost"
	if (-not (Test-Path $runOutput)) {
		$runOutput = Get-ChildItem (Join-Path $ConsoleDir "bin\Release\net10.0\runs\latest") -Directory | Select-Object -First 1 -ExpandProperty FullName
	}
	Copy-ScanResults $runOutput (Join-Path $AuditDir "FreeA11yChecker")
}

# ── Done ──

Write-Step "Audit complete"
Write-Host "  Results are in: $AuditDir"
Write-Host "  Review and commit with: git add A11yAudit/ && git commit -m 'Update a11y audit evidence'"
Write-Host ""
