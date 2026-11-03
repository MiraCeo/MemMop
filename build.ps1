<#
.SYNOPSIS
    Builds MemMop.

.DESCRIPTION
    The project targets .NET Framework 4.7.2, which every Windows 10 (1803+) and Windows 11 installs or is
    compatible with, so the result is a single executable that needs nothing beside it - no runtime download,
    no accompanying .dll. That is the point of staying on the framework.

    The screenshot tool is built too. It compiles the app's markup into its own exe, so skipping it after a
    XAML change leaves a tool that renders the previous interface without saying so.
#>
param(
    [string]$Configuration = "Release",
    [switch]$SkipTools
)

try { [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new() } catch { }

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "src\MemMop\MemMop.csproj"
$shot = Join-Path $root "tools\shot\Shot.csproj"
$dist = Join-Path $root "dist"

Push-Location $root

try {
    & dotnet build $project -c $Configuration --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "build failed with exit code $LASTEXITCODE" }

    if (-not $SkipTools) {
        & dotnet build $shot -c $Configuration --nologo -v q
        if ($LASTEXITCODE -ne 0) { throw "screenshot tool failed with exit code $LASTEXITCODE" }
    }

    $built = Join-Path $root "src\MemMop\bin\$Configuration\net472\MemMop.exe"
    if (-not (Test-Path $built)) { throw "expected output missing: $built" }

    New-Item -ItemType Directory -Force -Path $dist | Out-Null
    $target = Join-Path $dist "MemMop.exe"

    try {
        Copy-Item $built $target -Force -ErrorAction Stop
    }
    catch {
        Write-Host ""
        Write-Host "  cannot replace $target" -ForegroundColor Red
        Write-Host "  MemMop is probably still running - close it and build again." -ForegroundColor Red
        throw
    }

    $item = Get-Item $target
    $extra = Get-ChildItem (Split-Path $built -Parent) -File | Where-Object { $_.Extension -ne ".exe" } | Select-Object -ExpandProperty Name

    Write-Host ""
    Write-Host ("  output  : " + $item.FullName)
    Write-Host ("  version : " + $item.VersionInfo.ProductVersion)
    Write-Host ("  size    : " + [math]::Round($item.Length / 1KB, 1) + " KB")
    Write-Host ("  sha256  : " + (Get-FileHash $target -Algorithm SHA256).Hash)
    Write-Host ("  needs   : .NET Framework 4.7.2+ (installed on Windows 10 1803+ and Windows 11)")

    if ($extra) { Write-Host ("  beside  : " + ($extra -join ", ") + " (not required to run)") }

    if (-not $SkipTools) {
        Write-Host ("  shots   : " + (Join-Path $root "tools\shot\bin\$Configuration\net472\Shot.exe"))
    }
}
finally {
    Pop-Location
}
