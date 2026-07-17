param(
    [string]$Address = "2408:8244:510:17a4:9c31:aec8:e3c8:871b",
    [int]$Port = 17000,
    [string]$Email,
    [string]$Password,
    [string]$Checksum = "MobileProbeCheck2026",
    [int]$Seconds = 20,
    [int]$ConnectAttempts = 5,
    [int]$ConnectTimeoutSeconds = 12,
    [int]$CharacterIndex = -1
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

function Hex($bytes, [int]$max = 240) {
    if ($bytes.Length -eq 0) { return "" }
    $n = [Math]::Min($bytes.Length, $max)
    (($bytes[0..($n - 1)]) | ForEach-Object { $_.ToString("X2") }) -join " "
}

function Append-Bytes($list, $bytes, [int]$count) {
    for ($i = 0; $i -lt $count; $i++) { [void]$list.Add([byte]$bytes[$i]) }
}

function Try-Frame($list) {
    if ($list.Count -lt 4) { return $null }
    $arr = $list.ToArray()
    $len = [BitConverter]::ToInt32($arr, 0)
    if ($len -lt 6) { throw "bad length $len" }
    if ($list.Count -lt $len) { return $null }
    $raw = New-Object byte[] $len
    for ($i = 0; $i -lt $len; $i++) { $raw[$i] = $list[$i] }
    $list.RemoveRange(0, $len)
    $raw
}

function Connect-WithRetry($addr, [int]$port, [int]$attempts, [int]$timeoutSeconds) {
    for ($attempt = 1; $attempt -le $attempts; $attempt++) {
        $client = [System.Net.Sockets.TcpClient]::new([System.Net.Sockets.AddressFamily]::InterNetworkV6)
        $iar = $client.BeginConnect($addr, $port, $null, $null)
        if ($iar.AsyncWaitHandle.WaitOne([TimeSpan]::FromSeconds($timeoutSeconds))) {
            try {
                $client.EndConnect($iar)
                Write-Host "connect attempt=$attempt result=success"
                return $client
            }
            catch {
                Write-Host "connect attempt=$attempt result=error message=$($_.Exception.Message)"
                $client.Close()
            }
        }
        else {
            Write-Host "connect attempt=$attempt result=timeout"
            $client.Close()
        }
        Start-Sleep -Milliseconds 500
    }
    throw "Unable to connect [$addr]:$port after $attempts attempts."
}

function Decode-Login($raw) {
    $payloadLength = $raw.Length - 6
    $payload = New-Object byte[] $payloadLength
    [Array]::Copy($raw, 6, $payload, 0, $payloadLength)
    $ms = New-Object System.IO.MemoryStream(,$payload)
    $br = New-Object System.IO.BinaryReader($ms, [System.Text.Encoding]::UTF8)
    $result = $br.ReadByte()
    $message = $br.ReadString()
    $durationTicks = $br.ReadInt64()
    $chars = @()
    if ($ms.Position -lt $ms.Length) {
        $hasChars = $br.ReadBoolean()
        if ($hasChars) {
            $count = $br.ReadInt32()
            for ($i = 0; $i -lt $count; $i++) {
                $hasChar = $br.ReadBoolean()
                if (-not $hasChar) { continue }
                $idx = $br.ReadInt32()
                $name = $br.ReadString()
                $level = $br.ReadInt32()
                $gender = $br.ReadByte()
                $class = $br.ReadByte()
                $location = $br.ReadInt32()
                $lastLogin = $br.ReadInt64()
                $chars += [PSCustomObject]@{ Index=$idx; Name=$name; Level=$level; Gender=$gender; Class=$class; Location=$location; LastLoginBinary=$lastLogin }
            }
        }
    }
    [PSCustomObject]@{ Result=$result; Message=$message; DurationTicks=$durationTicks; Characters=$chars }
}

function Decode-StartGame($raw) {
    $payloadLength = $raw.Length - 6
    $payload = New-Object byte[] $payloadLength
    [Array]::Copy($raw, 6, $payload, 0, $payloadLength)
    $ms = New-Object System.IO.MemoryStream(,$payload)
    $br = New-Object System.IO.BinaryReader($ms, [System.Text.Encoding]::UTF8)
    $result = $br.ReadByte()
    $message = $br.ReadString()
    $durationTicks = $br.ReadInt64()
    [PSCustomObject]@{ Result=$result; Message=$message; DurationTicks=$durationTicks; PayloadLength=$payloadLength }
}

$md5 = [System.Security.Cryptography.MD5]::Create()
$hashBytes = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes("$Email-$Password"))
$passwordHash = ($hashBytes | ForEach-Object { $_.ToString("x2") }) -join ""

$pConnected = New-Packet 1 $null
$pPing = New-Packet 2 $null
$pLang = New-Packet 1007 { param($w) $w.Write("Chinese") }
$pLogin = New-Packet 1008 { param($w) $w.Write($Email); $w.Write($passwordHash); $w.Write($Checksum) }

$addr = [System.Net.IPAddress]::Parse($Address)
$client = Connect-WithRetry $addr $Port $ConnectAttempts $ConnectTimeoutSeconds
$client.ReceiveTimeout = 1000
$client.SendTimeout = 8000
$stream = $client.GetStream()
$buf = New-Object byte[] 16384
$pending = New-Object "System.Collections.Generic.List[byte]"
$sentLogin = $false
$sentStart = $false
$deadline = [DateTime]::UtcNow.AddSeconds($Seconds)

Write-Output "connected [$Address]:$Port"

while ([DateTime]::UtcNow -lt $deadline) {
    try {
        $n = $stream.Read($buf, 0, $buf.Length)
        if ($n -le 0) { Write-Output "remote_closed"; break }
        Append-Bytes $pending $buf $n
    }
    catch [System.IO.IOException] {}

    while ($true) {
        $raw = Try-Frame $pending
        if ($null -eq $raw) { break }
        $id = [BitConverter]::ToUInt16($raw, 4)
        Write-Output ("recv id={0} len={1} hex={2}" -f $id, $raw.Length, (Hex $raw))

        if ($id -eq 1) {
            $stream.Write($pConnected, 0, $pConnected.Length)
            Write-Output "send Connected"
        }
        elseif ($id -eq 2) {
            $stream.Write($pPing, 0, $pPing.Length)
            Write-Output "send Ping"
        }
        elseif ($id -eq 5 -and -not $sentLogin) {
            $stream.Write($pLang, 0, $pLang.Length)
            $stream.Write($pLogin, 0, $pLogin.Length)
            $sentLogin = $true
            Write-Output "send Login email=$Email passwordHash=$passwordHash checksum=$Checksum"
        }
        elseif ($id -eq 2003) {
            $login = Decode-Login $raw
            Write-Output "decode Login result=$($login.Result) message='$($login.Message)' characters=$($login.Characters.Count)"
            foreach ($c in $login.Characters) {
                Write-Output "character index=$($c.Index) name=$($c.Name) level=$($c.Level) gender=$($c.Gender) class=$($c.Class) location=$($c.Location)"
            }
            if ($login.Result -eq 10 -and -not $sentStart -and $login.Characters.Count -gt 0) {
                $selected = $CharacterIndex
                if ($selected -lt 0) { $selected = $login.Characters[0].Index }
                $pStart = New-Packet 1012 { param($w) $w.Write([Int32]$selected) }
                $stream.Write($pStart, 0, $pStart.Length)
                $sentStart = $true
                Write-Output "send StartGame characterIndex=$selected hex=$(Hex $pStart 64)"
            }
        }
        elseif ($id -eq 2012) {
            $start = Decode-StartGame $raw
            Write-Output "decode StartGame result=$($start.Result) message='$($start.Message)' payloadLength=$($start.PayloadLength)"
        }
    }
}

$client.Close()
Write-Output "closed"
