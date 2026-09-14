$ErrorActionPreference = "Stop"

$EmployeeCode = Read-Host "Enter FidelitasHub Employee Code for this Windows user"
if ([string]::IsNullOrWhiteSpace($EmployeeCode)) {
    throw "Employee Code is required."
}

$HubUrl = Read-Host "Enter FidelitasHub URL (press Enter for http://10.10.10.9:8080)"
if ([string]::IsNullOrWhiteSpace($HubUrl)) {
    $HubUrl = "http://10.10.10.9:8080"
}

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$PublishFolder = Join-Path $ScriptRoot "publish"
$Exe = Join-Path $PublishFolder "FidelitasHub.IdleMonitor.exe"

if (-not (Test-Path $Exe)) {
    throw "Monitor executable not found: $Exe`nPublish the IdleMonitor project first and copy its publish output into $PublishFolder"
}

$InstallFolder = Join-Path $env:LOCALAPPDATA "FidelitasHubIdleMonitor"
New-Item -ItemType Directory -Force -Path $InstallFolder | Out-Null
Copy-Item "$PublishFolder\*" $InstallFolder -Recurse -Force

$Settings = @{
    HubUrl = $HubUrl.TrimEnd('/')
    EmployeeCode = $EmployeeCode.Trim()
} | ConvertTo-Json

$SettingsPath = Join-Path $InstallFolder "settings.json"
$Settings | Set-Content -Path $SettingsPath -Encoding UTF8

$Startup = [Environment]::GetFolderPath("Startup")
$ShortcutPath = Join-Path $Startup "FidelitasHub Idle Monitor.lnk"
$Shell = New-Object -ComObject WScript.Shell
$Shortcut = $Shell.CreateShortcut($ShortcutPath)
$Shortcut.TargetPath = Join-Path $InstallFolder "FidelitasHub.IdleMonitor.exe"
$Shortcut.WorkingDirectory = $InstallFolder
$Shortcut.Save()

Start-Process (Join-Path $InstallFolder "FidelitasHub.IdleMonitor.exe")
Write-Host "Idle monitor installed for Windows user $env:USERNAME with FidelitasHub employee code $EmployeeCode."
