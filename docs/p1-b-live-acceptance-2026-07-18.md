# P1-B Live Server Acceptance

Date: 2026-07-18

## Result

The authorized reversible subset of P1-B passed on the physical Android device
against `192.168.0.100:17000`, using character `翻云覆雨` (level 100).

Inventory lock/unlock, inventory slot movement, equipment exchange, storage
deposit/withdrawal, and post-restart server consistency all passed. Every
temporary mutation was restored to its recorded baseline.

P1-B is not marked fully complete because actual NPC buy, sell, and repair,
stack merging, consumable use, and storage sorting could not be guaranteed
reversible with the current character state. The character had zero gold, and
the mobile UI has no stack-split or undo-sort operation.

## Baseline

- Map: 比奇县, coordinates `(161,232)`.
- Gold: `0`.
- World initialization: `items=26`, `skills=22`, `buffs=3`.
- Visible inventory: 11 occupied slots.
- Visible equipment: 8 occupied slots.
- Weapon: `破山剑`.
- Test inventory item: `回城卷`, inventory slot 0, count 670, unlocked.
- Storage: empty in the visible storage grid.

Baseline screenshots:

- `Builds/Android/DeviceQA/p1b_inventory_baseline_2026-07-18.png`
- `Builds/Android/DeviceQA/p1b_storage_baseline_2026-07-18.png`

## Inventory Mutations

### Lock and unlock

- Sent ItemLock for inventory slot 0 with `locked=true`.
- Server returned packet 2061 with inventory slot 0 and `locked=true`.
- Sent ItemLock with `locked=false`.
- Server returned packet 2061 with inventory slot 0 and `locked=false`.
- Final UI returned to `Lock`, and restart verification confirmed the item was
  not left locked.

Evidence:

- `Builds/Android/DeviceQA/p1b_item_locked_2026-07-18.png`
- `Builds/Android/DeviceQA/p1b_item_unlocked_2026-07-18.png`

### Slot movement and restoration

- Moved `回城卷 670` from inventory slot 0 to empty inventory slot 11.
- Server ItemMove packet 2059 reported `Inventory 0 -> Inventory 11`,
  `success=true`.
- Moved it back from slot 11 to slot 0.
- Server reported `Inventory 11 -> Inventory 0`, `success=true`.
- Count remained 670.

Evidence:

- `Builds/Android/DeviceQA/p1b_item_moved_2026-07-18.png`
- `Builds/Android/DeviceQA/p1b_item_restored_2026-07-18.png`

## Equipment Mutation And Restoration

- The generic ItemUse command for `影魅之刃` was sent, but the server did not
  mutate equipment or return an equipment change. This path was therefore
  treated as unsupported/rejected without side effects.
- The native grid-move path moved inventory slot 1 (`影魅之刃`) to equipment
  slot 0, swapping `破山剑` into inventory slot 1.
- Server ItemMove packet 2059 reported
  `Inventory 1 -> Equipment 0`, `success=true`.
- Repeating the same grid move with `破山剑` restored the original weapon and
  returned `影魅之刃` to inventory slot 1; the server again reported success.

Evidence:

- `Builds/Android/DeviceQA/p1b_weapon_move_result_2026-07-18.png`
- `Builds/Android/DeviceQA/p1b_weapon_restored_2026-07-18.png`

## Storage Mutation And Restoration

- Moved `回城卷 670` from inventory slot 0 to storage slot 0.
- Server ItemMove packet 2059 reported
  `Inventory 0 -> Storage 0`, `success=true`.
- Moved it back from storage slot 0 to inventory slot 0.
- Server reported `Storage 0 -> Inventory 0`, `success=true`.
- The item count remained 670 and storage returned to empty.

Evidence:

- `Builds/Android/DeviceQA/p1b_storage_deposit_2026-07-18.png`
- `Builds/Android/DeviceQA/p1b_storage_restored_2026-07-18.png`

## NPC Service Validation

- With no item selected, Sell and Repair were disabled.
- Selecting `回城卷 670` enabled Sell and kept Repair disabled.
- The default sale amount was 1.
- Selecting damaged `影魅之刃` displayed durability `8832/20000` and enabled
  ordinary Repair.
- Special repair and guild-funds toggles remained off.
- No sell, buy, or repair command was sent because gold was zero and an exact
  restoration path was unavailable.

Evidence:

- `Builds/Android/DeviceQA/p1b_npc_service_baseline_2026-07-18.png`
- `Builds/Android/DeviceQA/p1b_npc_scroll_selected_2026-07-18.png`
- `Builds/Android/DeviceQA/p1b_npc_weapon_selected_2026-07-18.png`

## Restart Consistency

- The application was force-stopped and cold-started in a new process.
- After logging back in as `翻云覆雨`, StartGame again reported
  `items=26`, `skills=22`, and `buffs=3` at `(161,232)`.
- Inventory slot 0 still contained `回城卷 670`.
- Inventory slot 1 still contained `影魅之刃`.
- Equipment slot 0 still contained `破山剑`.
- Storage was still empty.
- No fatal exception, native crash, or ANR was recorded.

Evidence:

- `Builds/Android/DeviceQA/p1b_relogin_world_2026-07-18.png`
- `Builds/Android/DeviceQA/p1b_relogin_inventory_2026-07-18.png`
- `Builds/Android/DeviceQA/p1b_relogin_storage_2026-07-18.png`

## Deferred P1-B Cases

The following cases remain deferred rather than failed:

- NPC buy/sell/ordinary repair mutation: requires disposable gold/items or
  explicit acceptance that the original economy state cannot be restored.
- Stack merge: the current UI has no stack-split operation for restoration.
- Consumable use: consumes an item or changes character location/state.
- Storage sort: the current UI has no undo operation for the prior ordering.

These cases can be closed with a server-prepared disposable economy state, or
formally accepted as environmental limitations before proceeding to P1-C.
