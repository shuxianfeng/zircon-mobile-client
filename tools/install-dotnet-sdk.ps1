param(
    [string]$Channel = "8.0",
    [string]$InstallDir = ".tools\dotnet"
)

$ErrorActionPreference = "Stop"

$root = (Resolve-Path ".").Path
$installPath = Join-Path $root $InstallDir
$scriptPath = Join-Path $env:TEMP "dotnet-install.ps1"
$downloadUrl = "https://dot.net/v1/dotnet-install.ps1"

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

if (-not (Test-Path -LiteralPath $installPath)) {
    New-Item -ItemType Directory -Path $installPath | Out-Null
}

Write-Host "download url=$downloadUrl"
Invoke-WebRequest -Uri $downloadUrl -OutFile $scriptPath

Write-Host "install channel=$Channel path=$installPath"
& powershell -ExecutionPolicy Bypass -File $scriptPath -Channel $Channel -InstallDir $installPath -NoPath

$dotnetExe = Join-Path $installPath "dotnet.exe"
if (-not (Test-Path -LiteralPath $dotnetExe)) {
    throw "dotnet.exe was not created at $dotnetExe"
}

& $dotnetExe --info


