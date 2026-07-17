# Phase 7 Mobile Touch Controls

Date: 2026-07-08

## Current Result

Added the first mobile movement input component:

- `Assets/Scripts/Game/Input/ZirconTouchMovementBehaviour.cs`

Runtime behavior:

- Requires a `ZirconProtocolProbeBehaviour` reference.
- Sends movement only when the probe is in `InGame` state.
- Converts single-finger drag direction into the PC-compatible `MirDirection` order:
  - `0 Up`
  - `1 UpRight`
  - `2 Right`
  - `3 DownRight`
  - `4 Down`
  - `5 DownLeft`
  - `6 Left`
  - `7 UpLeft`
- Sends `Client.Move` at a configurable repeat interval.
- Optionally references `ZirconMapDebugRenderer` and skips moves into loaded blocking cells before sending packets.
- Supports WASD and arrow keys in editor for fast local validation.

## Probe Integration

`ZirconProtocolProbeBehaviour` now exposes reusable command methods:

- `StartCharacterAsync()`
- `SendTurnCommandAsync(byte direction)`
- `SendMoveCommandAsync(byte direction, int distance)`
- `SendPickUpCommandAsync(byte type)`
- `SendChatCommandAsync(string text)`
- `IsInGame`
- `GetWorldSnapshot()`

The IMGUI debug buttons and mobile input component share the same command path, so movement validation logs are consistent.

## Next Live Server Validation

This phase will need a live server session after the user switches to an IPv6-capable network and starts the online service.

Validation target:

1. Connect and login.
2. Start a character.
3. Use `ZirconTouchMovementBehaviour` or the editor keyboard fallback to send `Client.Move`.
4. Confirm the server replies with movement/location packets such as `ObjectMove`, `ObjectTurn`, or `DataObjectLocation`.
5. Confirm `ZirconWorldState` updates the local player position and `ZirconWorldCameraFollow` follows it.

No server or PC client code changes are required for this validation.

## Remaining Work

- Add NPC tap-to-interact and dialog response handling.
- Add pickup tap/nearby-item behavior.
- Add skill button input once magic/skill payloads are decoded.
- Replace prototype input with final mobile HUD controls after the gameplay packet loop is stable.

## Target Selection And Normal Attack

Implemented on 2026-07-14:

- Added explicit Client.Attack packet 1018 encoding: Direction byte, MirAction byte, MagicType Int32.
- Added ZirconNetworkClient.SendAttackAsync and probe-level SendAttackCommandAsync.
- Added ZirconTargetCombatBehaviour for mobile tap selection and normal attack.
- Single tap selects the nearest live player or monster and applies a renderer highlight.
- Double tap attacks the selected target; editor Tab selects the next nearby target and Space attacks.
- Attack direction is calculated from the local map coordinate to the selected target using the existing MirDirection order.
- Selection clears automatically when the target dies, disappears, or is no longer represented by a selectable entity packet.
- Added command-line probe options --attack-direction and --attack-magic for UI-free live validation after StartGame.

Release build verification remains 0 warnings and 0 errors for Zircon.ProtocolProbe and Zircon.AssetPipeline. Live movement and normal attack response validation passed on 2026-07-14. Unity compilation remains pending.
## NPC And Pickup Interaction

Implemented and live-validated on 2026-07-14:

- Single tap on an NPC sends Client.NPCCall 1031 with a one-second repeat guard.
- Server.NPCResponse 2070 is decoded into object ID and page index.
- Server.NPCClose 2076 clears the NPC dialog state.
- ZirconWorldSnapshot exposes the active NPC dialog state for the future mobile panel.
- PickUpNearbyAsync sends the existing range-based Client.PickUp sequence command; editor P is available as a temporary test input.

The live server returned NPCResponse object 902, page 148. Pickup packet encoding is implemented, but a live dropped-item acceptance test still requires a known item on the ground near the test character.
## Skill And Ground Item Pipeline

Implemented on 2026-07-14:

- StartGame decodes ClientUserMagic entries including InfoIndex, keys, level, experience, and remaining cooldown.
- The real client System.db is parsed into a 145-entry MagicInfo manifest with MagicType, class, mode, icon, cost, delay, and description.
- Client.Magic 1020 is encoded with direction, Spell action, MagicType, target object, and map point.
- Server ObjectMagic 2024, MagicToggle 2042, NewMagic 2048, MagicLeveled 2049, and MagicCooldown 2050 are decoded.
- ZirconMobileSkillButtonBehaviour can be bound to a uGUI Button and supports Free, Pose, Point, Direction, and Block targeting data.
- Server ObjectItem 2037 and DataObjectItem 2168 create dropped-item world entities.
- Server ItemsGained 2058 and ObjectRemove provide the acceptance signals for a successful pickup.

Local Release builds pass with 0 warnings and 0 errors. The System.db export passes against the real 7,474,362-byte database. Live skill and pickup acceptance both passed after IPv6 connectivity recovered.
## Live Skill Validation

Validated against the live IPv6 server on 2026-07-14:

- StartGame decoded 22 learned skills for the warrior character.
- InfoIndex 12 mapped through the exported System.db manifest to MagicType 111, Iron Shirt, Pose mode.
- Client.Magic 1020 was sent with no target at the local character position.
- Server.MagicCooldown 2050 returned InfoIndex 12.
- Server.ObjectMagic 2024 returned MagicType 111 with Cast=True.
- StatsUpdate increased from 47 to 48 entries after the buff became active.
- The default mobile skill button configuration now uses this validated skill.

A real drop-and-pickup loop was validated on 2026-07-14. The probe decoded 26 inventory entries, dropped one unit from a safe stack, received ItemChanged, ObjectItem, and DataObjectItem, then received ItemsGained and ObjectRemove after Client.PickUp. The original stack was restored and Phase 7 live pickup acceptance is complete.