$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Project = Join-Path $Root "FidelitasHub.IdleMonitor\FidelitasHub.IdleMonitor.csproj"
$Output = Join-Path $Root "publish"

dotnet publish $Project `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -o $Output

Write-Host "Monitor publish completed: $Output"
