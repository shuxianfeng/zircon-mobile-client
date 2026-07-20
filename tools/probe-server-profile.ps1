param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("home", "company")]
    [string]$Location,
    [int]$TimeoutSeconds = 20
)

$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$dotnet = Join-Path $projectRoot ".tools\dotnet\dotnet.exe"
$probeProject = Join-Path $projectRoot "tools\Zircon.ProtocolProbe\Zircon.ProtocolProbe.csproj"
$hostName = if ($Location -eq "home") { "192.168.0.100" } else { "zircon.35861344.xyz" }
$port = 17000

if (-not (Test-Path -LiteralPath $dotnet)) {
    throw "Project-local dotnet is missing: $dotnet"
}

$env:DOTNET_CLI_HOME = Join-Path $projectRoot ".tools\dotnet-home"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"

Write-Output "profile=$Location endpoint=$hostName`:$port mode=handshake-only"
& $dotnet run --project $probeProject -c Release -- `
    --host $hostName `
    --port $port `
    --timeout $TimeoutSeconds `
    --no-hex
exit $LASTEXITCODE

