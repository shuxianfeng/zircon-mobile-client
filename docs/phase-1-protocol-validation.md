# Phase 1 Protocol Validation

Date: 2026-07-07

## Goal

Validate that an independent mobile-side client can connect to the existing online Zircon service and begin the PC-compatible handshake.

Target:

- Host: `zircon.35861344.xyz`
- Port: `17000`

## Implementation Added

| Path | Purpose |
| --- | --- |
| `Assets/Scripts/Core/Protocol/ZirconPacketIds.cs` | Fixed packet IDs for initial mobile implementation. |
| `Assets/Scripts/Core/Protocol/ZirconBinary.cs` | Packet frame encoding, frame reading, MD5, and hex logging helpers. |
| `Assets/Scripts/Core/Protocol/ZirconClientPackets.cs` | Explicit client packet writers for `Connected`, `Ping`, `Version`, `SelectLanguage`, `Login`, `LoginSimple`, and `StartGame`. |
| `Assets/Scripts/Core/Network/ZirconConnectionState.cs` | Mobile connection state machine seed. |
| `tools/Zircon.ProtocolProbe` | Standalone TCP/protocol probe source. |

## Connectivity Result

Command run:

```powershell
Test-NetConnection zircon.35861344.xyz -Port 17000
```

Result:

```text
Name resolution of zircon.35861344.xyz failed
```

The same result was returned after approved unrestricted network execution.

## Current Technical Conclusion

The current blocker is DNS resolution for `zircon.35861344.xyz`, not packet format or login protocol. No evidence has been collected yet that the service accepts or rejects the mobile handshake because the host did not resolve.

## Required Next Input or External Change

One of these is needed to continue live protocol validation:

- DNS for `zircon.35861344.xyz` becomes resolvable from this machine.
- A direct IP address for the online server is provided.
- A replacement host/port is provided.

## Mobile-Side Workaround Plan

1. Keep the mobile probe host configurable via `--host` and `--port`.
2. Keep `Version.ClientHash` configurable via `--client-hash` or `--client-binary`.
3. Keep login optional so handshake can be tested without account credentials.
4. If version hash fails later, compute MD5 from the actual PC executable and retry from the mobile probe before considering any server change.

## Commands To Re-run

Handshake only:

```powershell
dotnet run --project tools/Zircon.ProtocolProbe -- --host zircon.35861344.xyz --port 17000
```

With PC executable hash:

```powershell
dotnet run --project tools/Zircon.ProtocolProbe -- --client-binary "path\to\Client.exe"
```

With login:

```powershell
dotnet run --project tools/Zircon.ProtocolProbe -- --email account@example.com --password plaintextPassword
```

## Local Tooling Limitation

This machine currently reports `.NET SDKs were not found`, so `dotnet run` cannot execute here yet. The source is ready for a machine with .NET SDK or for later Unity integration.

## Phase 1 Acceptance Status

| Criterion | Status |
| --- | --- |
| Can connect to `zircon.35861344.xyz:17000` | Blocked by DNS resolution failure |
| Can complete handshake or locate failure reason | Located current failure before handshake: DNS |
| Can complete login or locate failure reason | Pending DNS/connectivity and credentials |
| Can receive character list or locate failure reason | Pending successful login |
| No server modification | Satisfied |
| No PC client modification | Satisfied |
