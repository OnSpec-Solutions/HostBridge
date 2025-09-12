param(
    [string]$Solution = "",
    [string]$Output = "coverage.dcvr",
    [string]$Report = "coverage.cobertura.xml",
    [string]$Filters = "",
    [string]$Framework = "",
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'

function Write-Info($msg) {
    if (-not $Quiet) { Write-Host "[dotCover] $msg" -ForegroundColor Cyan }
}

function Find-DotCoverExe {
    if ($env:DOTCOVER_EXE -and (Test-Path $env:DOTCOVER_EXE)) { return $env:DOTCOVER_EXE }
    $candidates = @()
    $candidates += (Get-Command dotCover.exe -ErrorAction SilentlyContinue)?.Source

    $local = "$env:LOCALAPPDATA\JetBrains";
    if (Test-Path $local) {
        $candidates += Get-ChildItem -Path $local -Filter dotCover64.exe -Recurse -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
        $candidates += Get-ChildItem -Path $local -Filter dotCover.exe -Recurse -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
    }

    foreach ($c in $candidates) {
        if ($c -and (Test-Path $c)) { return $c }
    }
    throw "dotCover executable not found. Set DOTCOVER_EXE env var or put dotCover.exe on PATH."
}

if (-not $Solution) {
    # default to HostBridge.sln in repo root (script assumed to be in tools/dotCover)
    $repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    $Solution = Join-Path $repoRoot 'HostBridge.sln'
}

$dotCover = Find-DotCoverExe
Write-Info "Using dotCover at: $dotCover"

# Build VSTest command line that runs all tests in solution via vstest.console if available, else 'dotnet test'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

# Prefer 'dotnet test' to keep it simple across frameworks; dotCover can wrap any process
$dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue)?.Source
if (-not $dotnet) { throw "dotnet SDK not found; required to run tests under coverage." }

$dotnetArgs = @('test', 'HostBridge.sln', '--no-build')
if ($Framework) { $dotnetArgs += @('-f', $Framework) }

$filtersArg = $Filters
if (-not $filtersArg) {
    # Exclude tests and examples by default
    $filtersArg = '-:module=*.Tests;-:module=*.Examples;-:class=*.Generated*'
}

# Ensure output paths are absolute
$Output = Resolve-Path -LiteralPath (Join-Path (Get-Location) $Output)
$Report = Resolve-Path -LiteralPath (Join-Path (Get-Location) $Report) -ErrorAction SilentlyContinue
if (-not $Report) { $Report = (Join-Path (Get-Location) 'coverage.cobertura.xml') }

Write-Info "Running tests under coverage..."

# Build dotCover cover command
# dotCover.exe cover /TargetExecutable=dotnet /TargetArguments="test HostBridge.sln --no-build" /Output=coverage.dcvr /Filters="+:module=*;-:module=*.Tests"

$coverArgs = @('cover')
$coverArgs += "/TargetExecutable=$dotnet"
$coverArgs += "/TargetArguments=\"$($dotnetArgs -join ' ')\""
$coverArgs += "/Output=$Output"
$coverArgs += "/Filters=$filtersArg"

& $dotCover $coverArgs

Write-Info "Exporting Cobertura report to $Report"
$reportArgs = @('report', "/Source=$Output", "/Output=$Report", '/ReportType=Cobertura')
& $dotCover $reportArgs

Write-Info "Done. Snapshot: $Output, Report: $Report"