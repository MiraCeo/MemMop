<#
.SYNOPSIS
    Builds MemMop.

.DESCRIPTION
    Tries a single-file publish first. That needs the win-x64 runtime pack from NuGet; when the machine has no
    feed access the script falls back to a framework-dependent build, which produces a folder instead of one
    file and needs the .NET 8 Desktop Runtime on the target machine.
#>
param(
    [string]$Configuration = "Release"
)

try { [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new() } catch { }

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "src\MemMop\MemMop.csproj"
$dist = Join-Path $root "dist"

Push-Location $root

try {
    $single = Join-Path $dist "single"
    Remove-Item $single -Recurse -Force -ErrorAction SilentlyContinue

    & dotnet publish $project -c $Configuration -r win-x64 --self-contained false -p:PublishSingleFile=true -o $single --nologo -v q | Out-Null
    $published = (Test-Path (Join-Path $single "MemMop.exe")) -and ((Get-ChildItem $single -File | Measure-Object).Count -le 3)

    if ($published) {
        $target = Join-Path $dist "MemMop.exe"
        Copy-Item (Join-Path $single "MemMop.exe") $target -Force
        $note = "single file"
    }
    else {
        Write-Host "  single-file publish unavailable (needs the win-x64 runtime pack from NuGet); building folder output" -ForegroundColor DarkYellow

        & dotnet build $project -c $Configuration --nologo -v q | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "build failed with exit code $LASTEXITCODE" }

        $built = Join-Path $root "src\MemMop\bin\$Configuration\net8.0-windows"
        $target = Join-Path $dist "MemMop-1.0.0"
        New-Item -ItemType Directory -Force -Path $target | Out-Null
        Copy-Item (Join-Path $built "*") $target -Recurse -Force
        $target = Join-Path $target "MemMop.exe"
        $note = "folder output - requires the .NET 8 Desktop Runtime"
    }

    $item = Get-Item $target

    Write-Host ""
    Write-Host ("  output  : " + $item.FullName)
    Write-Host ("  version : " + $item.VersionInfo.ProductVersion)
    Write-Host ("  size    : " + [math]::Round($item.Length / 1KB, 1) + " KB")
    Write-Host ("  sha256  : " + (Get-FileHash $target -Algorithm SHA256).Hash)
    Write-Host ("  note    : " + $note)
}
finally {
    Pop-Location
}
