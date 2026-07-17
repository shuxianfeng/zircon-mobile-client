param(
    [string]$PcClientRoot = "E:\codex ide\zircon-legend-client",
    [string]$OutputPath = "docs\generated\packet-schema-summary.md"
)

$ErrorActionPreference = "Stop"

function Resolve-SourcePath {
    param([string]$RelativePath)
    $path = Join-Path $PcClientRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing source file: $path"
    }

    return $path
}

function Read-PacketFile {
    param(
        [string]$Kind,
        [string]$Path
    )

    $lines = Get-Content -LiteralPath $Path
    $packets = New-Object System.Collections.Generic.List[object]

    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -notmatch '\[PacketMark\((\d+)\)\]') {
            continue
        }

        $packetId = [int]$Matches[1]
        $classLine = $null
        $classLineNumber = 0

        for ($j = $i + 1; $j -lt $lines.Count; $j++) {
            if ($lines[$j] -match 'public\s+sealed\s+class\s+(\w+)\s*:\s*Packet') {
                $classLine = $lines[$j]
                $classLineNumber = $j
                $className = $Matches[1]
                break
            }
        }

        if ($null -eq $classLine) {
            continue
        }

        $properties = New-Object System.Collections.Generic.List[string]
        $braceDepth = 0
        $seenClassBrace = $false

        for ($k = $classLineNumber; $k -lt $lines.Count; $k++) {
            $line = $lines[$k]
            foreach ($char in $line.ToCharArray()) {
                if ($char -eq '{') {
                    $braceDepth++
                    $seenClassBrace = $true
                }
                elseif ($char -eq '}') {
                    $braceDepth--
                }
            }

            if ($line -match 'public\s+(.+?)\s+(\w+)\s*\{\s*get;\s*set;\s*\}') {
                $properties.Add("$($Matches[1]) $($Matches[2])")
            }

            if ($seenClassBrace -and $braceDepth -le 0) {
                break
            }
        }

        $packets.Add([pscustomobject]@{
            Kind = $Kind
            Id = $packetId
            Name = $className
            Properties = @($properties)
        })
    }

    return $packets
}

$sources = @(
    @{ Kind = "General"; Path = Resolve-SourcePath "Library\Library\Network\GeneralPackets.cs" },
    @{ Kind = "Client"; Path = Resolve-SourcePath "Library\Library\Network\ClientPackets.cs" },
    @{ Kind = "Server"; Path = Resolve-SourcePath "Library\Library\Network\ServerPackets.cs" }
)

$allPackets = New-Object System.Collections.Generic.List[object]
foreach ($source in $sources) {
    foreach ($packet in (Read-PacketFile -Kind $source.Kind -Path $source.Path)) {
        $allPackets.Add($packet)
    }
}

$outputFullPath = Join-Path (Get-Location) $OutputPath
$outputDir = Split-Path -Parent $outputFullPath
if (-not (Test-Path -LiteralPath $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

$builder = New-Object System.Text.StringBuilder
[void]$builder.AppendLine("# Generated Packet Schema Summary")
[void]$builder.AppendLine()
[void]$builder.AppendLine("Source root: ``$PcClientRoot``")
[void]$builder.AppendLine()
[void]$builder.AppendLine("| Kind | ID | Name | Properties |")
[void]$builder.AppendLine("| --- | ---: | --- | --- |")

foreach ($packet in ($allPackets | Sort-Object Kind, Id)) {
    $properties = if ($packet.Properties.Count -gt 0) { ($packet.Properties -join "<br>") } else { "" }
    [void]$builder.AppendLine("| $($packet.Kind) | $($packet.Id) | ``$($packet.Name)`` | $properties |")
}

Set-Content -LiteralPath $outputFullPath -Value $builder.ToString() -Encoding UTF8
Write-Host "generated path=$outputFullPath packets=$($allPackets.Count)"
