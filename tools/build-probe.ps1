param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$root = (Resolve-Path ".").Path
$dotnet = Join-Path $root ".tools\dotnet\dotnet.exe"

if (-not (Test-Path -LiteralPath $dotnet)) {
    throw "Missing local dotnet SDK. Run tools\install-dotnet-sdk.ps1 first."
}

$env:DOTNET_ROOT = Split-Path -Parent $dotnet
$env:DOTNET_CLI_HOME = Join-Path $root ".tools"
$env:NUGET_PACKAGES = Join-Path $root ".tools\nuget"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

& $dotnet build "tools\Zircon.ProtocolProbe\Zircon.ProtocolProbe.csproj" -c $Configuration -v:minimal
& $dotnet build "tools\Zircon.AssetPipeline\Zircon.AssetPipeline.csproj" -c $Configuration -v:minimal
& $dotnet run --project "tools\Zircon.ProtocolTests\Zircon.ProtocolTests.csproj" -c $Configuration
& $dotnet build "tools\Zircon.UnityUiCompileCheck\Zircon.UnityUiCompileCheck.csproj" -c $Configuration -v:minimal

