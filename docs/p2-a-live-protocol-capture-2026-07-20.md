# P2-A Live Protocol Capture

Date: 2026-07-20

## Scope And Safety

The company endpoint was exercised from the desktop protocol probe without a
mobile device. Credentials are not stored in the repository or this report.
Raw packet hex was disabled. No item use/drop, trade, mail, market, storage,
guild, purchase, or other economy mutation was sent.

## Authentication And Character Selection

- Login result: `Success`.
- Character list count: 3.
- The protocol's `StartGame` argument is the character database index, not the
  zero-based list position. Sending zero returned `StartGame.NotFound`.
- `tools/Zircon.LoginInspector` was added to print the real character indices
  safely from environment-provided credentials.
- The lowest-level character was selected for the remaining capture.

## Initial State And Entities

- `StartGame`: success.
- Character object: map 6, initial location `(145,169)`.
- Initial collections decoded: 20 items, 8 learned skills, and buffs.
- Player, monster, NPC, spell/effect, stats, weight, durability, currency, and
  surrounding object-location packets were decoded.
- Surrounding `ObjectTurn`, `ObjectMove`, and `DataObjectLocation` traffic was
  observed with object IDs, directions, coordinates, map index, and distance.

## Controlled Action Evidence

- Movement: sent `Client.Move(direction=2, distance=1)`. The same connection
  did not echo the player's own move, but the next authenticated StartGame
  location changed from `(145,169)` to `(146,169)`, confirming acceptance.
- Normal attack: sent `Client.Attack(direction=0, magic=0)`. Server returned
  `ObjectAttack` for the active player with direction, target, magic, element,
  and coordinates.
- Spell: the learned-skill InfoIndex 27 maps through the production System.db
  manifest to non-damaging Repulsion MagicType 205. Server returned
  `MagicCooldown(infoIndex=27)` and `ObjectMagic(type=205, cast=True)`.

## Remaining Live Gate

Map-transition field capture is intentionally deferred. The current character
is on map 6 and the headless probe has no visual knowledge of a safe portal or
route. It should be captured after a known portal coordinate is supplied or
during later device-based visual acceptance, rather than moving blindly or
using an unknown administrator command.

Android rendering, animation, input, loading-time, memory, frame-pacing,
screenshot, and logcat acceptance still require the physical device later.
