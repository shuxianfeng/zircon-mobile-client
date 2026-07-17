param(
    [string]$Address = "2408:8244:510:17a4:9c31:aec8:e3c8:871b",
    [int]$Port = 17000,
    [string]$Email = "mobile-probe@example.com",
    [string]$Password = "probe-password",
    [string]$Checksum = "MobileProbeCheck2026",
    [int]$Seconds = 12
)

$ErrorActionPreference = "Stop"

function New-Packet([UInt16]$id, [ScriptBlock]$payload) {
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)
    $bw.Write([UInt16]$id)
    if ($payload) { & $payload $bw }
    $body = $ms.ToArray()

    $out = New-Object System.IO.MemoryStream
    $ow = New-Object System.IO.BinaryWriter($out)
    $ow.Write([Int32]($body.Length + 4))
    $ow.Write($body)
    $out.ToArray()
}

function Hex($bytes, [int]$max = 220) {
    if ($bytes.Length -eq 0) { return "" }
    $n = [Math]::Min($bytes.Length, $max)
    (($bytes[0..($n - 1)]) | ForEach-Object { $_.ToString("X2") }) -join " "
}

function Append-Bytes($list, $bytes, [int]$count) {
    for ($i = 0; $i -lt $count; $i++) {
        [void]$list.Add([byte]$bytes[$i])
    }
}

function Try-Frame($list) {
    if ($list.Count -lt 4) { return $null }
    $arr = $list.ToArray()
    $len = [BitConverter]::ToInt32($arr, 0)
    if ($len -lt 6) { throw "bad length $len" }
    if ($list.Count -lt $len) { return $null }

    $raw = New-Object byte[] $len
    for ($i = 0; $i -lt $len; $i++) {
        $raw[$i] = $list[$i]
    }
    $list.RemoveRange(0, $len)
    $raw
}

$md5 = [System.Security.Cryptography.MD5]::Create()
$hashBytes = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes("$Email-$Password"))
$passwordHash = ($hashBytes | ForEach-Object { $_.ToString("x2") }) -join ""

$pConnected = New-Packet 1 $null
$pPing = New-Packet 2 $null
$pLang = New-Packet 1007 { param($w) $w.Write("Chinese") }
$pLogin = New-Packet 1008 {
    param($w)
    $w.Write($Email)
    $w.Write($passwordHash)
    $w.Write($Checksum)
}

$addr = [System.Net.IPAddress]::Parse($Address)
$client = [System.Net.Sockets.TcpClient]::new([System.Net.Sockets.AddressFamily]::InterNetworkV6)
$client.ReceiveTimeout = 1000
$client.SendTimeout = 8000
$client.Connect($addr, $Port)

$stream = $client.GetStream()
$buf = New-Object byte[] 8192
$pending = New-Object "System.Collections.Generic.List[byte]"
$sentLogin = $false
$deadline = [DateTime]::UtcNow.AddSeconds($Seconds)

Write-Output "connected [$Address]:$Port"

while ([DateTime]::UtcNow -lt $deadline) {
    try {
        $n = $stream.Read($buf, 0, $buf.Length)
        if ($n -le 0) {
            Write-Output "remote_closed"
            break
        }
        Append-Bytes $pending $buf $n
    }
    catch [System.IO.IOException] {
    }

    while ($true) {
        $raw = Try-Frame $pending
        if ($null -eq $raw) { break }

        $id = [BitConverter]::ToUInt16($raw, 4)
        Write-Output ("recv id={0} len={1} hex={2}" -f $id, $raw.Length, (Hex $raw))

        if ($id -eq 1) {
            $stream.Write($pConnected, 0, $pConnected.Length)
            Write-Output "send Connected hex=$(Hex $pConnected 64)"
        }
        elseif ($id -eq 2) {
            $stream.Write($pPing, 0, $pPing.Length)
            Write-Output "send Ping hex=$(Hex $pPing 64)"
        }
        elseif ($id -eq 5 -and -not $sentLogin) {
            $stream.Write($pLang, 0, $pLang.Length)
            Write-Output "send SelectLanguage hex=$(Hex $pLang 64)"
            $stream.Write($pLogin, 0, $pLogin.Length)
            $sentLogin = $true
            Write-Output "send Login email=$Email passwordHash=$passwordHash checksum=$Checksum len=$($pLogin.Length) hex=$(Hex $pLogin)"
        }
        elseif ($id -eq 2003 -or $id -eq 2182 -or $id -eq 7) {
            $deadline = [DateTime]::UtcNow
            break
        }
    }
}

$client.Close()
Write-Output "closed"
