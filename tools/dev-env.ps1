$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$toolsRoot = Join-Path $projectRoot ".tools"
$dotnetRoot = Join-Path $toolsRoot "dotnet"
$gitRoot = Join-Path $toolsRoot "git"
$unityRoot = Join-Path $toolsRoot "Unity\2022.3.62f1\Editor"
$asciiProjectRoot = "D:\ZirconMobileDev"
$androidRoot = Join-Path $unityRoot "Data\PlaybackEngines\AndroidPlayer"
$sdkRoot = Join-Path $androidRoot "SDK"
$ndkRoot = Join-Path $androidRoot "NDK"
$jdkRoot = Join-Path $androidRoot "OpenJDK"

$requiredFiles = @(
    (Join-Path $dotnetRoot "dotnet.exe"),
    (Join-Path $gitRoot "cmd\git.exe"),
    (Join-Path $unityRoot "Unity.exe"),
    (Join-Path $sdkRoot "platform-tools\adb.exe"),
    (Join-Path $jdkRoot "bin\java.exe")
)

foreach ($file in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $file)) {
        throw "Missing development dependency: $file"
    }
}

$env:DOTNET_ROOT = $dotnetRoot
$env:ZIRCON_UNITY_PROJECT = if (Test-Path -LiteralPath $asciiProjectRoot) { $asciiProjectRoot } else { $projectRoot }
$env:JAVA_HOME = $jdkRoot
$env:ANDROID_HOME = $sdkRoot
$env:ANDROID_SDK_ROOT = $sdkRoot
$env:ANDROID_NDK_ROOT = $ndkRoot
$env:GIT_SSL_CAINFO = Join-Path $gitRoot "mingw64\etc\ssl\certs\ca-bundle.crt"
$env:DOTNET_CLI_HOME = $toolsRoot
$env:NUGET_PACKAGES = Join-Path $toolsRoot "nuget"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

$pathEntries = @(
    (Join-Path $gitRoot "cmd"),
    $dotnetRoot,
    (Join-Path $jdkRoot "bin"),
    (Join-Path $sdkRoot "platform-tools")
)
$env:PATH = (($pathEntries + $env:PATH) -join [IO.Path]::PathSeparator)

Write-Host "Zircon development environment loaded."
Write-Host "Project: $projectRoot"
Write-Host "Unity:   $unityRoot\Unity.exe"
Write-Host "Build:   $env:ZIRCON_UNITY_PROJECT"
Write-Host "Android: $sdkRoot"
Write-Host "Usage: dotnet --version; git --version; adb devices"
