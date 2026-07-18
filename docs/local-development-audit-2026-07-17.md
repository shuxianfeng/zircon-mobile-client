# Local Development Audit

Date: 2026-07-19

## Current Position

The repository has completed P0 physical-device acceptance plus the safe,
single-player portions of P1-A, P1-B, and P1-C. It is no longer at the initial
protocol-prototype stage described by the root README.

- Phases 0 and 1 are complete. Login, character selection, StartGame, movement,
  normal attack, skill casting, and a real drop/pickup loop were live-validated.
- Phase 2 has a working Unity project skeleton, network state, packet dispatch,
  logs, UI flow, Android build automation, and a generated scene.
- Phases 3 and 4 cover the core packet families and gameplay state. Advanced or
  less frequently used protocol/model families still require live acceptance.
- Phase 5 has working image, map, and System.db conversion samples. Production
  atlases, audio, compression, and chunked delivery remain.
- Phase 6 has a real-map/entity MVP, camera follow, collision lookup, and Y
  sorting. Production layers, pivots, animation, effects, and shadows remain.
- Phase 7 implementation and live acceptance are complete.
- Phases 8 and 9 have broad controller/state coverage. P0 physical-device
  panel/navigation/keyboard/back-button acceptance is complete.
- Phase 10 has update/catalog/hash/cache infrastructure but still needs release
  acceptance.
- Phase 11 standalone regression gates pass. Release, performance, reconnect,
  and long-running device gates remain.

Recommended next work:

1. P1 batch A is complete: chat channels, MagicKey persistence, and Buff events.
2. P1 batch B's reversible subset is complete: inventory lock/move, equipment
   exchange, storage deposit/withdrawal, and restart consistency passed and all
   temporary changes were restored. NPC commerce/repair mutation, stack merge,
   consumable use, and storage sort remain deferred because the current
   zero-gold state does not provide an exact restoration path.
3. P1 batch C's single-player, read-only, and reversible subset is complete:
   generated Quest/Social/Mail/Market UI, quest tracking and restart persistence,
   group-permission switching, safe action gating, market search, and UI-touch
   isolation passed on device.
4. To close the remaining P1 cases, prepare a second disposable character plus
   disposable gold/items/mail and explicitly authorize guild, trade, mail, and
   market mutations. Otherwise proceed directly to P2 production map,
   animation, effects, audio, and asset delivery.
5. Finish P3 performance, stability, signing, branding, and release gates.

## Installed Local Toolchain

- Project-local .NET SDK: `8.0.422` at `.tools/dotnet`.
- Project-local MinGit: `2.55.0.windows.3` at `.tools/git`.
- Unity Hub: `C:\Program Files\Unity Hub\Unity Hub.exe`.
- Unity editor files: `.tools/Unity/2022.3.62f1/Editor`.
- Android Build Support installed into the editor.
- OpenJDK: `11.0.14.1`.
- Android NDK: `23.1.7779620` (r23b).
- Android SDK platforms: API 33, 34, and 35.
- Android Build Tools: `34.0.0`.
- Android Platform Tools / ADB: `32.0.0`.

Load all project-local command-line tools in a PowerShell session:

```powershell
powershell -ExecutionPolicy Bypass
. .\tools\dev-env.ps1
```

## Verification Performed

- `Zircon.ProtocolProbe` Release build: passed with 0 warnings and 0 errors.
- `Zircon.AssetPipeline` Release build: passed with 0 warnings and 0 errors.
- Protocol/world regression tests: 14/14 passed.
- Offline Unity UI compile check: passed with 0 warnings and 0 errors.
- JDK, ADB, NDK, SDK platforms, and Build Tools versions were verified directly.
- ADB daemon starts successfully, and Xiaomi 22041211AC / Android 14 is
  authorized and available as device `JV9PNRJF8HVSQ4UG`.

## Remaining Environment Gates

### Unity license and editor variant

`ProjectSettings/ProjectVersion.txt` requests international Unity
`2022.3.62f1 (4af31df58517)`. Unity's CDN redirected this machine to the China
mirror and installed product version `2022.3.62f1c1`. Official alternate Unity
hosts were also redirected to the China mirror.

Unity Hub authorization and the China Unity license are active. The `f1c1`
editor successfully imported all 2,373 assets, compiled the project, and built
the ARM64 IL2CPP Android package with exit code 0.

Android tooling rejects the original Chinese workspace path. A durable junction
at `D:\ZirconMobileDev` points to the real workspace and is now the required
Unity/Android build entry. The source files remain in their original location.

### Physical device

Xiaomi 22041211AC / Android 14 is authorized through ADB. The newly built APK was
installed after removing an older package with an incompatible signature. Cold
launch succeeded in 401 ms and the Unity ARM64 IL2CPP process remained alive
without a managed or native crash.

The login screen renders successfully. The former pre-login world-status text
and background bar have been fixed to follow the HUD's `LoadingMap` / `InGame`
visibility boundary. The updated APK was rebuilt, installed, cold-launched, and
visually verified on the authorized device without a fatal exception, native
crash, or ANR.

### Repository metadata

MinGit is installed and its archive passed the SHA-256 published in the official
Git for Windows release. The workspace still has no `.git` directory because a
TLS-verified GitHub connection was reset while reading the remote. Do not create
a disconnected local history. Restore the real remote metadata when GitHub is
reachable.
