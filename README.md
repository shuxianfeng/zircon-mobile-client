# Zircon Mobile Client

Zircon Legend/Mir3 mobile client implementation workspace.

Current milestone: `Mobile Protocol Login Prototype`.

The project is intentionally starting with protocol compatibility before rendering or full UI work. The mobile client must adapt to the existing online server and PC client behavior; this repository must not modify server code or PC client code without explicit user approval.

## Current Outputs

- `Assets/Scripts/Core/Protocol`: Unity-friendly protocol frame and packet helpers.
- `Assets/Scripts/Core/Network`: connection state model for the mobile client.
- `tools/Zircon.ProtocolProbe`: standalone C# TCP/protocol probe for handshake and login validation.
- `docs`: phase audit, core schema, and validation notes.

## Probe Usage

This machine currently has only a .NET runtime, not a .NET SDK, so the probe source is present but cannot be built here until an SDK or Unity build environment is available.

When .NET SDK is available:

```powershell
dotnet run --project tools/Zircon.ProtocolProbe -- --host zircon.35861344.xyz --port 17000 --timeout 30
```

With login credentials:

```powershell
dotnet run --project tools/Zircon.ProtocolProbe -- --email account@example.com --password plainPassword
```

The probe hashes plaintext passwords as `MD5(email + "-" + password)`, matching the PC client login behavior found in `Client\Scenes\LoginScene.cs`.

## Guardrails

- Do not modify the online server by default.
- Do not modify the PC client by default.
- Keep packet field order explicit in mobile code.
- Log raw packet IDs, lengths, parse status, and hex samples during every protocol test.

PowerShell probe for machines without .NET SDK:

```powershell
powershell -ExecutionPolicy Bypass -File tools\probe-login.ps1
```
