# Phase 9 Inventory, Equipment, Buffs, And NPC UI

Date: 2026-07-14

## Implemented Protocol And State

- StartInformation now initializes the full item list and active ClientBuffInfo list.
- Inventory slots and the 1000-based equipment offset are normalized into GridType plus slot state.
- ItemsGained, ItemMove, ItemChanged, ItemLock, and ItemDurability update ZirconWorldState.
- BuffAdd, BuffRemove, BuffChanged, BuffTime, and BuffPaused update timed buff state.
- Client ItemMove, ItemUse, ItemLock, NPCButton, NPCBuy, and NPCClose encoders are exposed through ZirconNetworkClient and ZirconProtocolProbeBehaviour.
- ZirconWorldSnapshot now publishes sorted inventory, equipment, and buff collections.

## Implemented Mobile Controllers

- ZirconInventoryEquipmentPanelBehaviour builds stable 49-slot inventory and 16-slot equipment grids from a reusable uGUI button template.
- A selected item can be moved by tapping a destination slot, used from inventory, or locked and unlocked.
- ZirconBuffBarBehaviour presents active effects, remaining time, paused state, and stat details.
- ZirconNpcDialogPanelBehaviour resolves the current page, parses the PC client's `[Text:ButtonID]` syntax, sends page actions, lists shop goods, buys one item, and closes the dialog.
- ZirconSystemCatalogBehaviour resolves item and NPC metadata from generated TextAsset manifests.

## Offline System.db Export

The real client System.db now exports:

- 145 magic definitions to `magics.manifest.json`.
- 1,376 item definitions to `items.manifest.json`.
- 1,006 NPC pages, 1,373 button relationships, and 514 shop-good rows to `npc-pages.manifest.json`.

## Verification

- Protocol and world-state regression tests: 9 passed.
- ProtocolProbe and AssetPipeline Release builds: 0 warnings, 0 errors.
- The four new uGUI scripts compile through Zircon.UnityUiCompileCheck with 0 warnings and 0 errors.
- No Unity Editor, game client, or live server connection was used.

## Remaining Acceptance

1. Assemble the controllers, templates, and generated TextAssets in a Unity scene or prefabs.
2. Import and bind item and buff icon sprites.
3. Validate item move/use/lock, equipment changes, Buff events, NPC buttons, and one purchase against the live IPv6 server.
4. Add advanced NPC sell, repair, storage, refinement, and quantity dialogs.
5. Continue Android asset delivery and package/device verification.