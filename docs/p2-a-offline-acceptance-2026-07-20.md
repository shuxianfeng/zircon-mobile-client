# P2-A Offline And Build Acceptance

Date: 2026-07-20

## Result

The non-device P2-A gates pass. Production visual metadata, atlases, Android
resource chunks, Unity compilation, IL2CPP, Gradle packaging, and APK content
were verified. Live-server and physical-device presentation gates remain open.

## Regression And Unity Checks

- Protocol/world regression tests: 14/14 passed.
- Offline Unity UI compile check: 0 warnings and 0 errors.
- The compile-check harness was extended for the P2 skill-preview API and the
  skill-button clear operation introduced after P1.
- Unity batch compilation and Runtime scene diagnostics completed without a
  missing script or C# compiler error.

## Production Visual Validation

`ZirconP2AValidation.ValidateFromCommandLine` rebuilds the production visual
manifest and atlases, then checks required sprite-set counts, atlas rectangles,
bundle mappings, and atlas assets.

Validated result:

- Sprite sets: 8.
- Required frames: 168.
- Atlas assets: 3.
- Android bundle mappings: 3.
- Required chunks: `zircon-p2-character`, `zircon-p2-entities`, and
  `zircon-p2-effects`.

## Android Build

- Android target: ARM64, IL2CPP.
- Unity/Tundra native build: passed.
- Gradle release build: passed.
- APK: `Builds/Android/ZirconMobile.apk`.
- APK size: 77,834,648 bytes.
- APK SHA-256:
  `C7A89FA2244666DD17D88BC315A5AD5F3FB90397EBC9F1A0FFB75063AB03A389`.
- APK archive inspection confirmed the production visual manifest and all
  three P2 Android chunks are packaged under `assets/Zircon`.

Gradle spent several minutes waiting on Google/Maven metadata through the
current company network before completing from the available dependency cache.
This was an environment delay rather than a source or packaging failure.

## Server Connectivity Gate

The current client endpoint is `192.168.0.100:17000`. From the company network:

- TCP connection to `192.168.0.100:17000`: timed out.
- Ping to `192.168.0.100`: timed out.
- `zircon.35861344.xyz`: DNS resolution failed.
- Local active IPv4 networks are `192.168.1.0/24` and `192.168.240.0/24`.
- Tailscale lists the workstation and an offline phone, but no server node or
  subnet-router path to `192.168.0.0/24`.

No account login or character mutation was attempted. Live P2 state-field
capture remains blocked until a routable server endpoint, VPN/subnet route, or
public DNS endpoint is available.

## Remaining P2-A Gates

1. Connect to a routable live server and capture map, entity model, movement,
   attack, spell, and map-transition fields without economy mutations.
2. Install this APK on an Android device.
3. Confirm runtime AssetBundle logs for all three chunks.
4. Visually validate the female-warrior animation, monster sample animation,
   transparency shader, shadows, spell preview, offsets, and Y sorting.
5. Capture loading time, memory, frame pacing, screenshots, and logcat evidence.

