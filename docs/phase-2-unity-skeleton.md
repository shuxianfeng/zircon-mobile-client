# Phase 2 Unity Skeleton

Date: 2026-07-08

## Implemented

- Created Unity-compatible project folders:
  - `Assets/Scripts/Core/Protocol`
  - `Assets/Scripts/Core/Network`
  - `Assets/Scripts/Core/Models`
  - `Assets/Scripts/Game/World`
  - `Assets/Scripts/Game/Entities`
  - `Assets/Scripts/Game/Combat`
  - `Assets/Scripts/UI/Login`
  - `Assets/Scripts/UI/Hud`
  - `Assets/Scripts/UI/Windows`
  - `Assets/Generated/Textures`
  - `Assets/Generated/Atlases`
  - `Assets/Generated/Data`
- Added Unity package manifest and project version marker.
- Added `ZirconNetworkClient`:
  - IPv6-capable TCP connect.
  - Auto-replies `Connected` and `Ping`.
  - Handles `CheckVersion` if the server sends it.
  - Emits packet and state events.
  - Sends `SelectLanguage`, `Login`, and `StartGame`.
- Added `ZirconProtocolProbeBehaviour` as a temporary runtime UI for connection/login/character/start-game validation.

## Current Runtime Expectations

1. Open the project in Unity 2022.3 LTS or newer.
2. Create a temporary scene.
3. Add an empty GameObject.
4. Attach `ZirconProtocolProbeBehaviour`.
5. Fill test credentials in the Inspector at runtime.
6. Use the on-screen buttons to connect, login, and optionally start the first character.

## Notes

- Test credentials are intentionally not hardcoded in Unity assets.
- This is not the final mobile UI; it is a protocol validation surface for Phase 2.
- The first real mobile UI should replace IMGUI with uGUI/TextMeshPro panels after the protocol flow is stable.
