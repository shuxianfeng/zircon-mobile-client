# Development Plan Status

Date: 2026-07-15

## Position In The Original Plan

The original plan defines phases 0 through 11. Current implementation has completed the Phase 7 live acceptance gates and entered Phase 8.

- Phase 0 baseline audit: completed.
- Phase 1 protocol compatibility prototype: completed and previously proven through login, character list, StartGame, and first in-game packet batch.
- Phase 2 Unity skeleton: core folders, network state, packet dispatch, logs, and debug UI are implemented. Unity Editor and Android launch acceptance are still pending.
- Phase 3 packet schema: handshake, login, StartGame, movement, core entity, chat, combat, inventory/equipment, skill, core buff, and core NPC packet families are implemented. Advanced storage, trade, repair, refinement, quest, and companion families remain partial.
- Phase 4 shared models: world/entity/status, inventory, equipment, skills, buffs, and NPC dialog state are implemented. Storage state is implemented. Quests, trade, and companion models remain.
- Phase 5 asset pipeline: Zl image and map sample conversion are implemented and validated. Atlases, audio, Android texture compression, and chunked map delivery remain.
- Phase 6 map/entity rendering MVP: real sample tiles and sprites, camera follow, collision lookup, and Y sorting are implemented. Complete map layers, metadata offsets, shadows, action animations, and Android asset delivery remain.
- Phase 7 mobile controls: movement, target selection, normal attack, NPC interaction, nearby pickup, skill-list decoding, skill runtime state, and a reusable mobile skill button are implemented. Live skill-cast and real drop/pickup acceptance are complete.
- Phase 8 UI: formal uGUI login, character selection, UI flow, main HUD, inventory/equipment, Buff bar, NPC dialog, chat, skill book/hotbar binding, and storage controller scripts are implemented. Scene/prefab visual assembly and real Unity compilation remain pending; offline uGUI compile checking passes.
- Phase 11: standalone tool builds are verified; Unity/Android/device release gates are not yet verified.

## Current Execution Order

1. Assemble and visually verify the Phase 8 login, character selection, main HUD, and first skill button in Unity.
2. Assemble the completed inventory/equipment, Buff, and NPC controllers into Unity prefabs.
3. Build sell, repair, quantity, and advanced NPC views; then continue quests, minimap, social, trade, mail, and market.
4. Replace prototype file loading with Android-ready asset delivery and complete map/action rendering.
5. Perform Unity compilation, Android packaging, device performance, reconnect, and stability tests.

## Live Validation Result

On 2026-07-14 the backend resumed sending the Zircon handshake. Login, StartGame, one-step movement, local-player location correction, normal attack, ObjectAttack 2022, and CombatTime 2091 were all validated successfully through the command-line probe. Phase 7 movement, combat, NPC interaction, skill, and pickup acceptance are complete.
## Phase 7 And 8 Update

Implemented on 2026-07-14:

- StartInformation now decodes the character ClientUserMagic list after inventory, belt, and auto-potion links.
- System.db MagicInfo export produced 145 entries at Assets/Generated/Data/System/magics.manifest.json.
- Client.Magic 1020 encoding and ZirconNetworkClient.SendMagicAsync are implemented.
- ObjectMagic, MagicToggle, NewMagic, MagicLeveled, and MagicCooldown decoders are implemented.
- ObjectItem, DataObjectItem, and ItemsGained decoders provide both sides of pickup acceptance.
- ZirconWorldState now tracks skills, cooldowns, spell actions, and dropped-item entities.
- ZirconMobileSkillButtonBehaviour provides target-aware casting and cooldown feedback for uGUI.
- Formal login, character selection, UI flow, and main HUD controller scripts have started Phase 8.
- The previous debug overlay and automatic startup connection are disabled by default.

IPv6 connectivity later recovered. Skill casting and a real drop/pickup loop were validated through the command-line probe. No Unity or game window was opened.
## Live Skill Acceptance

On 2026-07-14, after IPv6 connectivity recovered, the probe decoded 22 learned warrior skills and successfully cast Iron Shirt using MagicType 111. The server returned MagicCooldown for InfoIndex 12, ObjectMagic with Cast=True, and a subsequent StatsUpdate containing one additional stat entry. Phase 7 skill-list and first-skill acceptance are complete.

The probe also decoded the real 26-item inventory, selected a safe stacked item, dropped one unit, and immediately picked it back up. The server returned ItemChanged, ObjectItem, DataObjectItem, ItemsGained, and ObjectRemove for the same item/object. Phase 7 live acceptance is complete.
## Inventory, Equipment, Buff, And NPC Update

Implemented offline on 2026-07-14:

- StartGame and incremental world state now cover inventory, equipment, and active Buffs.
- Mobile controllers cover a 49-slot bag, 16 equipment slots, item move/use/lock, Buff timers, NPC text/buttons, shop goods, purchase, and close.
- System.db exports 1,376 item definitions, 1,006 NPC pages, 1,373 NPC button relationships, and 514 NPC goods.
- Client and server protocol coverage was extended for core item, Buff, and NPC actions.
- Release protocol/world tests pass 9/9, and the new uGUI scripts pass the offline Unity API compile check.

Live server acceptance is intentionally deferred until the user next enables the IPv6 service. Unity scene/prefab assembly and real Unity/Android compilation remain open.
## Chat, Skill Book, And Storage Update

Implemented offline on 2026-07-14:

- Mobile chat supports all six PC-compatible channel payload formats.
- The generated magic catalog now drives a learned-skill list and four-position hotbar binding UI.
- MagicKey 1045 is encoded and exposed through the live session layer with optimistic world-state updates.
- Login storage items are preserved across StartGame and exposed to UI.
- Mobile storage supports inventory/storage moves and server-side sorting.
- SortStorageItem 1120/2187 and StorageSize 2175 are covered by regression tests.
- Protocol/world tests pass 10/10 and the expanded offline uGUI compile check passes with 0 warnings and 0 errors.

This batch does not require port 17000. Live validation of chat routing, key persistence, and storage mutations is deferred until the IPv6 service is enabled.
## Offline Phase 8-10 Expansion (2026-07-15)

All implementation work that can currently be verified without IPv6 and without a Unity Editor has been continued:

- Advanced NPC sell/repair and quantity controls are implemented through compatible packet/state/UI layers.
- Initial and incremental quests, task tracking/completion, and a nearby-entity minimap are implemented.
- Group, basic guild, trade, mail, and market core flows have compatible packet/state/UI layers.
- System.db export now adds 46 quests, 42 quest tasks, and 289 maps to the existing magic/item/NPC catalogs.
- Android ARM64/IL2CPP build automation, permissions, StreamingAssets staging, resource cataloging, SHA-256 verification, retry, cache, and update code are implemented.
- Protocol/world tests pass 14/14; all standalone builds and the expanded uGUI compile check pass with 0 warnings and 0 errors.

The next gates are external rather than additional port-17000-independent coding: Unity scene/prefab assembly, real Unity compilation, APK generation/signing, device testing, and then live protocol acceptance. Unity Editor is not installed or detectable on this workstation, so those gates remain pending. Port 17000 can remain closed until live acceptance begins.