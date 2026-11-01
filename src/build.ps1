<#
.SYNOPSIS
    Builds MemMop and writes the single-file executable.
#>
param(
    [string]$Configuration = "Release"
)

try { [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new() } catch { }

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "src\MemMop\MemMop.csproj"

Push-Location $root
try {
    & dotnet publish $project -c $Configuration -r win-x64 --self-contained false -p:PublishSingleFile=true -o (Join-Path $root "dist")
    if ($LASTEXITCODE -ne 0) { throw "build failed with exit code $LASTEXITCODE" }

    $exe = Join-Path $root "dist\MemMop.exe"
    $hash = (Get-FileHash $exe -Algorithm SHA256).Hash
    $version = (Get-Item $exe).VersionInfo.ProductVersion

    Write-Host ""
    Write-Host ("  built   : " + $exe)
    Write-Host ("  version : " + $version)
    Write-Host ("  size    : " + [math]::Round((Get-Item $exe).Length / 1KB, 1) + " KB")
    Write-Host ("  sha256  : " + $hash)
}
finally {
    Pop-Location
}
