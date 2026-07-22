param(
    [int]$TimeoutSeconds = 15,
    [string]$ExpectedInterface
)

$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$dotnet = Join-Path $projectRoot ".tools\dotnet\dotnet.exe"
$probeProject = Join-Path $projectRoot "tools\Zircon.ProtocolProbe\Zircon.ProtocolProbe.csproj"
$publicHost = "zircon.35861344.xyz"
$port = 17000

if (-not (Test-Path -LiteralPath $dotnet)) {
    throw "Project-local dotnet is missing: $dotnet"
}

$address = $null
$resolvedBy = $null
foreach ($dnsServer in @("223.5.5.5", "8.8.8.8")) {
    try {
        $address = Resolve-DnsName $publicHost -Type AAAA -Server $dnsServer -DnsOnly -ErrorAction Stop |
            Where-Object { $_.Type -eq "AAAA" } |
            Select-Object -First 1 -ExpandProperty IPAddress
        if ($address) {
            $resolvedBy = $dnsServer
            break
        }
    }
    catch {
        Write-Warning "AAAA lookup failed via $dnsServer`: $($_.Exception.Message)"
    }
}
if (-not $address) {
    throw "No public AAAA record returned for $publicHost"
}

$route = Find-NetRoute -RemoteIPAddress $address -ErrorAction SilentlyContinue |
    Where-Object { $_.InterfaceAlias } |
    Select-Object -First 1
$interfaceAlias = if ($route) { $route.InterfaceAlias } else { "unknown" }
$interfaceIndex = if ($route) { $route.InterfaceIndex } else { "unknown" }
if ($ExpectedInterface -and $interfaceAlias -ne $ExpectedInterface) {
    throw "Expected route interface '$ExpectedInterface', but Windows selected '$interfaceAlias'."
}

$env:DOTNET_CLI_HOME = Join-Path $projectRoot ".tools\dotnet-home"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
Write-Output "profile=company publicHost=$publicHost resolved=$address resolvedBy=$resolvedBy port=$port addressFamily=IPv6 interface=$interfaceAlias interfaceIndex=$interfaceIndex mode=handshake-only"

$lines = & $dotnet run --project $probeProject -c Release -- `
    --host $address `
    --port $port `
    --timeout $TimeoutSeconds `
    --ipv6 `
    --no-hex 2>&1
$lines | Write-Output
if ($lines -match "state=ReadyForLogin") {
    exit 0
}
exit $LASTEXITCODE
