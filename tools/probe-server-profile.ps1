param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("home", "company")]
    [string]$Location,
    [int]$TimeoutSeconds = 20,
    [string]$ExpectedInterface
)

$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$dotnet = Join-Path $projectRoot ".tools\dotnet\dotnet.exe"
$probeProject = Join-Path $projectRoot "tools\Zircon.ProtocolProbe\Zircon.ProtocolProbe.csproj"
$port = 17000

if ($Location -eq "company") {
    & (Join-Path $PSScriptRoot "probe-company-server.ps1") `
        -TimeoutSeconds $TimeoutSeconds `
        -ExpectedInterface $ExpectedInterface
    exit $LASTEXITCODE
}

$hostName = "192.168.0.100"
if (-not (Test-Path -LiteralPath $dotnet)) {
    throw "Project-local dotnet is missing: $dotnet"
}

$env:DOTNET_CLI_HOME = Join-Path $projectRoot ".tools\dotnet-home"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"

Write-Output "profile=home endpoint=$hostName`:$port addressFamily=IPv4 mode=handshake-only"
& $dotnet run --project $probeProject -c Release -- `
    --host $hostName `
    --port $port `
    --timeout $TimeoutSeconds `
    --no-hex
exit $LASTEXITCODE
