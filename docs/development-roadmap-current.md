# Current Development Roadmap

Date: 2026-07-17

This file is the durable execution reference for the remaining mobile-client work. Older phase notes remain useful as implementation history, but statements that Unity or Android builds have not run are obsolete.

## Verified Baseline

- Unity 2022.3.62f1 imports the project.
- The Android ARM64 IL2CPP build completes successfully.
- `Builds/Android/ZirconMobile.apk` has launched on a physical Android device without a recorded managed crash.
- Protocol and world-state regression tests pass 14/14.
- Login, character selection, StartGame, movement, normal attack, skill casting, and a real drop/pickup loop have previously passed live protocol validation.
- The generated Unity scene currently has functional login, character-selection, and basic HUD bindings.
- Most advanced feature panels have controller code but still require complete controls, templates, serialized references, visual layout, and live acceptance.

## P0 - First Playable Mobile Loop

1. Complete scene/prefab assembly and serialized references.
2. Add touch movement, target selection, normal attack, pickup, four skill buttons, chat, and Buff presentation to the main HUD.
3. Make inventory/equipment, skill book/hotbar, NPC dialog/services, and storage fully operable.
4. Add offline preview/mock-state paths where practical so UI can be checked without the live server.
5. Verify safe areas, keyboard obstruction, touch sizes, scrolling, back/close navigation, Unity compilation, APK build, and physical-device launch.

P0 acceptance loop:

`Launch -> Login -> Character Select -> Enter World -> Move -> Select/Attack -> Cast Skill -> Pick Up -> Inventory -> NPC`

## P1 - Live Server Acceptance

Validate chat channels, MagicKey persistence, inventory/equipment mutations, Buff events, NPC buy/sell/repair, storage operations, quests, group/guild/trade, mail, and market in small logged batches against the live IPv6 server. Server and PC-client changes remain out of scope unless explicitly approved.

## P2 - Production Map, Animation, And Assets

Complete map layers and chunk delivery, Zl offsets/pivots, correct model/library selection, character and monster actions, effects, shadows, occlusion/Y sorting, atlases, Android texture compression, audio, fonts, and production resource update delivery.

## P3 - Release And Stability

Complete reconnect/background/lock-screen/weak-network testing, soak and memory tests, low/mid-range device performance, graphics API compatibility, release signing/versioning/icons/splash, log redaction, upgrade installation, and resource-cache recovery.

## Required External Conditions

- P0 implementation and offline verification can proceed locally.
- P1 requires the IPv6 backend and port 17000, a disposable test account/character, and permission to exercise item, trade, mail, market, guild, and storage mutations.
- Device acceptance requires a stable authorized USB or wireless ADB connection. The latest USB trace reported Windows device error 433.
- P2 requires the complete licensed PC resource set and a visual reference for the intended client version.
- P3 requires the final package identity, branding, target distribution channel, and a securely supplied release keystore.

## Repository Safety

The workspace currently contains an empty `.git` directory rather than a usable Git repository. Establishing a recoverable version-control baseline is strongly recommended before broad scene and resource changes.
