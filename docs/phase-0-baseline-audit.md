# Phase 0 Baseline Audit

Date: 2026-07-07

## Scope

Reference projects:

- PC client: `E:\codex ide\zircon-legend-client`
- PC launcher: `E:\codex ide\zircon-legend-laucher`

Mobile workspace:

- `E:\codex ide\zircon-mobile-client`

No server or PC client files were modified.

## Protocol Entry Points

| File | Finding | Mobile action |
| --- | --- | --- |
| `Library\Library\Network\Packet.cs` | Packet format is 4-byte little-endian total length, 2-byte packet ID, then payload. Serialization uses reflection over public properties. | Reimplement explicit packet encoding in mobile code. Do not depend on reflection order. |
| `Library\Library\Network\GeneralPackets.cs` | General handshake packets are IDs 1-7: `Connected`, `Ping`, `CheckVersion`, `Version`, `GoodVersion`, `PingResponse`, `Disconnect`. | Implement first in mobile probe. |
| `Library\Library\Network\ClientPackets.cs` | Login-relevant client packets: `SelectLanguage` 1007, `Login` 1008, `StartGame` 1012, `CheckClientDb` 1109, `LoginSimple` 1111. | Encode field order manually. |
| `Library\Library\Network\ServerPackets.cs` | Login-relevant server packets: `Login` 2003, `StartGame` 2012, `CheckClientDb` 2179, `CheckClientHash` 2180, `LoginSimple` 2182. | Decode incrementally; unknown packets must not crash client. |
| `Library\Library\Enum.cs` | Enum underlying types vary. Login and disconnect results are byte enums. | Schema must record enum underlying width. |
| `Library\Library\Globals.cs` | Contains shared constants plus network payload models such as `SelectInfo`, `StartInformation`, `ClientUserItem`. Some models depend on `System.Drawing` and DB globals. | Migrate only protocol-required model fields, replacing desktop types. |

## PC Runtime Flow Findings

| Area | Finding | Evidence |
| --- | --- | --- |
| TCP connect | PC client resolves configured host, then opens `TcpClient`, then creates `CConnection`. | `Client\Scenes\LoginScene.cs` |
| Initial handshake | On server `Connected`, PC sends `Connected` back and marks `ServerConnected = true`. | `Client\Envir\CConnection.cs` |
| Version check | On `CheckVersion`, PC sends `Version { ClientHash = MD5(Application.ExecutablePath) }`. | `Client\Envir\CConnection.cs` |
| Post-version | On `GoodVersion`, PC shows login and sends `SelectLanguage { Language = Config.Language }`. | `Client\Envir\CConnection.cs` |
| Login password | When user types a password, PC sends `MD5(email + "-" + password)`. Remembered passwords are already hashed. | `Client\Scenes\LoginScene.cs` |
| CheckSum | PC stores a random 20-character string in `Documents\Zircon\CheckSum.bin` and sends it with login/account packets. | `Client\Envir\CEnvir.cs` |

## Desktop-Bound Areas

| File or area | Desktop dependency | Mobile action |
| --- | --- | --- |
| `Client\Envir\DXManager.cs` | SlimDX / Direct3D9 rendering | Rewrite in Unity. |
| `Client\Controls\DXControl.cs` | Desktop UI and DirectX control tree | Rewrite mobile UI. |
| `Client\Scenes\*.cs` | PC windows, mouse/keyboard flow, DX controls | Use only behavior reference; do not port directly. |
| `Client\Models\MirLibrary.cs` | Legacy image library loading plus desktop image/color assumptions | Build offline conversion pipeline. |
| `Library\Library\Globals.cs` models | `System.Drawing.Point`, `Size`, `Color`, DB completion hooks | Replace with Unity-safe structs or mobile models. |

## Reuse Classification

| Category | Items | Status |
| --- | --- | --- |
| Directly reusable | Packet ID values, primitive binary formats, basic handshake sequence | Reimplemented in `Assets/Scripts/Core/Protocol`. |
| Migratable with changes | Enums, `SelectInfo`, `StartInformation`, item/magic/buff models | Pending Phase 3/4 schema hardening. |
| Offline conversion needed | Mir image libraries, maps, audio/resources | Pending Phase 5. |
| Must rewrite | DirectX rendering, PC UI windows, mouse/keyboard-first interactions | Pending Unity phases. |
| Unknown risk | Exact reflection property order on target runtime, server version/hash policy, account/login availability | Needs packet capture or successful probe run. |

## First Validation Scope

1. DNS/TCP connection to `zircon.35861344.xyz:17000`.
2. Receive server `Connected`.
3. Send client `Connected`.
4. Receive `CheckVersion`.
5. Send `Version` with configurable client MD5 hash.
6. Receive `GoodVersion` or `Disconnect`.
7. Send `SelectLanguage`.
8. Optionally send `Login` or `LoginSimple`.
9. Log all packet IDs, lengths, parse status, and raw hex samples.

## Phase 0 Acceptance

- Protocol layer entry is identified.
- Asset format entry is identified as `MirLibrary` and related resources.
- PC-only dependencies are identified.
- First mobile validation scope is defined.
