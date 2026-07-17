# Offline Phase 8-10 Implementation

Date: 2026-07-15

## Scope Completed Without IPv6

### Advanced NPC services

- Added client `NPCSell` 1034 and `NPCRepair` 1036 encoders.
- Added server `ItemsChanged` 2071 and `NPCRepair` 2072 decoders and world-state mutation.
- Added a touch-oriented service panel with item selection, bounded quantity stepper, normal/special repair, and guild-fund options.

### Quests and minimap

- `StartInformation` now optionally decodes the real quest tail after Buffs without breaking truncated samples.
- Added `QuestChanged` 2151 plus accept, complete, and track commands 1087-1089.
- Added cloned quest/task world state and a mobile quest list/progress controller.
- Added a nearby-entity minimap radar based on the authoritative map index and coordinates.
- System.db export now includes 46 quests, 42 quest tasks, and 289 maps. Quest and map UI resolve real names and metadata.

### Social, trade, mail, and market

- Added group switch/invite/remove/response packets and group state.
- Added basic guild create, notice, member invite/kick, and invitation response packets.
- Added trade request/response/open/close, item, gold, confirm, and partner-state handling.
- Added mail list/new/delete/attachment updates plus open, collect, delete, compose, gold, and attachment commands.
- Added market search, listing, purchase, cancellation, consignment, history, and result-state handling.
- Added mobile social/trade, mail, and market controllers.

### Android and resource delivery preparation

- Added an ARM64, IL2CPP Android build entry point with Android 8 minimum SDK configuration.
- Added Android internet/network-state permissions.
- Added build-time staging of `Assets/Generated` into StreamingAssets.
- Added a 73-file SHA-256 resource catalog.
- Added bundled/cache asset loading and optional remote catalog update with retries, hash verification, atomic replacement, and cache clearing.

## Verification

- Protocol/world regression tests: 14/14 passed.
- Protocol probe Release build: 0 warnings, 0 errors.
- Asset pipeline Release build: 0 warnings, 0 errors.
- Expanded uGUI compile check: 0 warnings, 0 errors.
- Resource catalog: 73/73 files present and SHA-256 matched.
- No Unity Editor, game window, IPv6 connection, or port 17000 connection was used.

## Remaining External Gates

The workstation does not currently have a detectable Unity Editor installation. The following cannot be truthfully accepted from command-line source checks alone:

1. Assemble scenes/prefabs and assign all serialized references and TextAssets.
2. Import icons/atlases and visually verify safe-area layouts.
3. Run real Unity compilation and resolve Unity-only API/importer issues.
4. Produce the APK with Android SDK/NDK and verify signing.
5. Test on physical Android devices.
6. Validate all new packet families against the live server on port 17000.

No server or PC client changes were made.