# Phase 7 Live Move Validation Attempt

Date: 2026-07-08

## Scope

Company-computer safe validation only. No Unity editor, no game window, and no graphical client was opened.

Target:

1. Connect to the live Zircon TCP endpoint.
2. Login and start a character.
3. Send `Client.Move` from the command-line protocol probe.
4. Confirm server movement/location responses such as `ObjectMove`, `ObjectTurn`, or `DataObjectLocation`.

## Probe Updates

Updated `tools/Zircon.ProtocolProbe/Program.cs`:

- Added receive summaries through `ZirconPacketSummary.Describe`.
- Added `--no-hex` to avoid printing raw login packet bytes.
- Added movement options:
  - `--move-direction <0-7>`
  - `--move-sequence <csv>`
  - `--move-distance <value>`
  - `--move-count <value>`
- When `Server.StartGame` decodes as success, the probe can queue `Client.Move` packets without opening any UI.

Build verification:

```powershell
powershell -ExecutionPolicy Bypass -File tools\build-probe.ps1 -Configuration Release
```

Result: `Zircon.ProtocolProbe` and `Zircon.AssetPipeline` build with `0 warnings` and `0 errors`.

## Network Result

DNS check:

```powershell
Test-NetConnection -ComputerName zircon.35861344.xyz -Port 17000
```

Result: name resolution failed in the current environment.

Known IPv6 endpoint check:

```powershell
Test-NetConnection -ComputerName 2408:8244:510:17a4:9c31:aec8:e3c8:871b -Port 17000
```

Result: `TcpTestSucceeded : False`.

Command-line probe check without credentials:

```powershell
Zircon.ProtocolProbe.exe --host 2408:8244:510:17a4:9c31:aec8:e3c8:871b --port 17000 --ipv6 --timeout 10 --no-hex
```

Result:

```text
socket_error=NetworkUnreachable message=向一个无法连接的网络尝试了一个套接字操作。
```

## Conclusion

Live movement validation could not proceed because the local machine currently has no usable IPv6 route to the server endpoint. No login or movement packet was sent during this attempt.

No server code, PC client code, or Unity/game UI was opened or modified.

## Next Retry Condition

Retry this same command-line validation after the machine has a working IPv6 route to:

- `2408:8244:510:17a4:9c31:aec8:e3c8:871b:17000`

The next retry can stay command-line only and does not require opening Unity or the game window.

## Retry 2026-07-08 19:44 Asia/Shanghai

The user switched network again and requested another command-line-only retry.

No Unity editor, game client, or graphical window was opened.

DNS now resolves successfully:

```text
zircon.35861344.xyz AAAA 2408:8244:510:17a4:9c31:aec8:e3c8:871b
```

TCP probe with external network permission:

```powershell
Zircon.ProtocolProbe.exe --host 2408:8244:510:17a4:9c31:aec8:e3c8:871b --port 17000 --ipv6 --timeout 10 --no-hex
```

Result:

```text
timeout_seconds=10 state=Connecting
```

Conclusion: DNS and IPv6 address resolution are now OK, but TCP connection to port `17000` still times out. Login, StartGame, and Client.Move validation did not run, and no credentials or game packets were sent.

## Retry 2026-07-08 19:50 Asia/Shanghai

The user requested another command-line-only retry.

No Unity editor, game client, or graphical window was opened.

`Test-NetConnection` resolved the domain and selected an IPv6 source address:

```text
RemoteAddress : 2408:8244:510:17a4:9c31:aec8:e3c8:871b
InterfaceAlias: WLAN 2
SourceAddress : 2408:8445:502:36c4:643c:ac7c:15e6:f6ed
TcpTestSucceeded: False
```

Command-line probe result:

```text
timeout_seconds=10 state=Connecting
```

Conclusion: the local machine now has an IPv6 source address, but TCP connection to the server port `17000` still times out. No login, StartGame, or Client.Move packets were sent.

## Retry 2026-07-08 19:52 Asia/Shanghai

The user requested another command-line-only retry.

No Unity editor, game client, or graphical window was opened.

`Test-NetConnection` result:

```text
RemoteAddress : 2408:8244:510:17a4:9c31:aec8:e3c8:871b
InterfaceAlias: WLAN 2
SourceAddress : 2408:8445:502:36c4:643c:ac7c:15e6:f6ed
TcpTestSucceeded: False
```

Command-line probe result:

```text
timeout_seconds=10 state=Connecting
```

Conclusion: IPv6 source address is still present, but TCP connection to server port `17000` still times out. No login, StartGame, or Client.Move packets were sent.

## Retry 2026-07-08 19:55-19:56 Asia/Shanghai

The user confirmed the online server was started and requested another command-line-only retry.

No Unity editor, game client, or graphical window was opened.

`Test-NetConnection` result:

```text
RemoteAddress : 2408:8244:510:17a4:9c31:aec8:e3c8:871b
InterfaceAlias: WLAN 2
SourceAddress : 2408:8445:502:36c4:643c:ac7c:15e6:f6ed
TcpTestSucceeded: False
```

Command-line probe with 10 second timeout:

```text
timeout_seconds=10 state=Connecting
```

Command-line probe with 30 second timeout:

```text
socket_error=TimedOut message=由于连接方在一段时间后没有正确答复或连接的主机没有反应，连接尝试失败。
```

Conclusion: after the server was reportedly started, the client machine still has IPv6 source connectivity but TCP connection to server port `17000` continues to time out. No login, StartGame, or Client.Move packets were sent.

## Successful Validation 2026-07-14

Command-line-only validation was completed without opening Unity or a game window.

- TCP connected to the current IPv6 endpoint on port 17000.
- Received General.Connected and General.GoodVersion.
- Login succeeded and returned three characters.
- StartGame succeeded for character index 1.
- The local player entered map 1 at 161,229.
- Sent one Client.Move direction 2, distance 1.
- Received DataObjectLocation for the local player at 162,229.
- Received ObjectMove for the same local player, direction 2, distance 1.
- Sent one Client.Attack direction 2 with MagicType.None.
- Received Server.ObjectAttack 2022 and Server.CombatTime 2091.

Conclusion: the Phase 7 movement and normal-attack protocol loops are accepted by the live server. The mobile world model now decodes ObjectAttack and stores action, attack magic, target ID, direction, and corrected location.
## NPC Interaction Validation 2026-07-14

A second command-line-only session validated NPC interaction without opening Unity or a game window.

- Login and StartGame succeeded.
- The local player entered map 1 at 162,229.
- NPC object 902, NPC index 100, was observed at 172,229.
- Sent Client.NPCCall 1031 with object ID 902.
- Received Server.NPCResponse 2070 with object ID 902 and page index 148.

Conclusion: NPC tap interaction is protocol-compatible. The mobile world snapshot now exposes NpcDialogOpen, NpcObjectId, and NpcPageIndex for the future mobile NPC panel.