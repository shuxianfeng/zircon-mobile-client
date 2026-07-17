# Phase 1 IPv6 Connectivity Validation

Date: 2026-07-08

## Target

- Host: `zircon.35861344.xyz`
- Port: `17000`
- Network note: server is exposed through IPv6 reverse proxy/tunnel; IPv4-only networks cannot reach it.

## DNS Result

AAAA resolution succeeded:

```text
zircon.35861344.xyz AAAA 2408:8244:510:17a4:9c31:aec8:e3c8:871b
```

No A record was returned in the local test.

## TCP Result

`Test-NetConnection zircon.35861344.xyz -Port 17000 -InformationLevel Detailed` succeeded on the IPv6-capable mobile network:

```text
RemoteAddress           : 2408:8244:510:17a4:9c31:aec8:e3c8:871b
RemotePort              : 17000
NameResolutionResults   : 2408:8244:510:17a4:9c31:aec8:e3c8:871b
InterfaceAlias          : WLAN 2
SourceAddress           : 2408:8445:502:36c4:e10b:52e9:b07c:1c01
TcpTestSucceeded        : True
```

## Protocol First Packet Result

A minimal IPv6 TCP client connected and read the first server packet:

```text
connected remote=[2408:8244:510:17a4:9c31:aec8:e3c8:871b]:17000 firstRead=6 firstPacketId=1 hex=06 00 00 00 01 00
```

Packet ID `1` is `General.Connected`, matching `Library\Library\Network\GeneralPackets.cs` in the PC client.

The probe then sent the matching PC-compatible `Connected` response:

```text
06 00 00 00 01 00
```

A short 200ms wait did not receive the next packet. A longer wait script was attempted, but the local Windows permission layer denied execution twice before a protocol result was available.

## Conclusion

- IPv6 DNS is now working.
- TCP port `17000` is reachable.
- The Zircon protocol layer is responding with the expected initial `General.Connected` packet.
- Next validation target is `CheckVersion` / `Version` / `GoodVersion`.

## Next Steps

1. Run the standalone protocol probe when .NET SDK or Unity runtime is available.
2. Keep client hash configurable; if the server rejects version, retry with the PC executable MD5.
3. After `GoodVersion`, send `SelectLanguage` and then login packet with test credentials.

## Handshake and Login Probe

Reusable script:

```powershell
powershell -ExecutionPolicy Bypass -File tools\probe-login.ps1
```

Observed packet flow:

```text
connected [2408:8244:510:17a4:9c31:aec8:e3c8:871b]:17000
recv id=1 len=6 hex=06 00 00 00 01 00
send Connected hex=06 00 00 00 01 00
recv id=5 len=6 hex=06 00 00 00 05 00
send SelectLanguage hex=0E 00 00 00 EF 03 07 43 68 69 6E 65 73 65
send Login email=mobile-probe@example.com passwordHash=14f6f539e8ef3217c814f3a17c286455 checksum=MobileProbeCheck2026 len=85
recv id=2 len=6 hex=06 00 00 00 02 00
send Ping hex=06 00 00 00 02 00
recv id=2180 len=11 hex=0B 00 00 00 84 08 01 00 00 00 00
recv id=2003 len=21 hex=15 00 00 00 D3 07 03 00 00 00 00 00 00 00 00 00 00 00 00 00 00
closed
```

Packet interpretation:

| Packet ID | Meaning | Result |
| --- | --- | --- |
| 1 | `General.Connected` | Server accepted TCP and began Zircon handshake. |
| 5 | `General.GoodVersion` | Current server path did not require explicit `CheckVersion` / `Version`. |
| 2 | `General.Ping` | Client must keep responding with packet ID 2. |
| 2180 | `Server.CheckClientHash` | Server sent client hash/update metadata after login attempt. |
| 2003 | `Server.Login` | Payload starts with `03`, which maps to `LoginResult.AccountNotExists`; expected for the controlled nonexistent test account. |

Updated conclusion:

- Login packet encoding is accepted by the server.
- The controlled nonexistent account produced the expected server-side account lookup failure.
- Next validation target is a real test account, character list decoding, and `StartGame`.
