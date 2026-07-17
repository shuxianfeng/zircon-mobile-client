param(
    [Parameter(Mandatory = $true)]
    [string]$Source,

    [string]$Output = "Assets\Generated\Textures",
    [string]$Type = "image",
    [string]$Indices = "",
    [int]$Max = 16,
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$root = (Resolve-Path ".").Path
$dotnet = Join-Path $root ".tools\dotnet\dotnet.exe"
$project = Join-Path $root "tools\Zircon.AssetPipeline\Zircon.AssetPipeline.csproj"

if (-not (Test-Path -LiteralPath $dotnet)) {
    throw "Missing local dotnet SDK. Run tools\install-dotnet-sdk.ps1 first."
}

if (-not (Test-Path -LiteralPath $Source)) {
    throw "Missing source library: $Source"
}

$env:DOTNET_ROOT = Split-Path -Parent $dotnet
$env:DOTNET_CLI_HOME = Join-Path $root ".tools"
$env:NUGET_PACKAGES = Join-Path $root ".tools\nuget"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

$args = @(
    "run",
    "--project", $project,
    "-c", $Configuration,
    "--",
    "--source", $Source,
    "--output", $Output,
    "--type", $Type,
    "--max", $Max
)

if (-not [string]::IsNullOrWhiteSpace($Indices)) {
    $args += @("--indices", $Indices)
}

& $dotnet @args
