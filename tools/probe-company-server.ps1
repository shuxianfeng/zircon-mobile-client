param([int]$TimeoutSeconds = 15)

$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$dotnet = Join-Path $projectRoot ".tools\dotnet\dotnet.exe"
$probeProject = Join-Path $projectRoot "tools\Zircon.ProtocolProbe\Zircon.ProtocolProbe.csproj"
$publicHost = "zircon.35861344.xyz"
$port = 17000

$address = Resolve-DnsName $publicHost -Type AAAA -Server 223.5.5.5 -ErrorAction Stop |
    Where-Object { $_.Type -eq "AAAA" } |
    Select-Object -First 1 -ExpandProperty IPAddress
if (-not $address) { throw "No AAAA record returned for $publicHost" }

$env:DOTNET_CLI_HOME = Join-Path $projectRoot ".tools\dotnet-home"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
Write-Output "profile=company publicHost=$publicHost resolved=$address port=$port mode=handshake-only"

$lines = & $dotnet run --project $probeProject -c Release -- `
    --host $address --port $port --timeout $TimeoutSeconds --ipv6 --no-hex 2>&1
$lines | Write-Output
if ($lines -match "state=ReadyForLogin") { exit 0 }
exit $LASTEXITCODE

