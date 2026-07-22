# P2-A4 And P2-A5 Live Validation

Date: 2026-07-22

## Scope And Safety

The company endpoint was reached over IPv6 through `WLAN 2`. Credentials were
provided only through process environment variables and are not stored in the
repository. Raw packet hex was not logged. No item, currency, market, mail,
trade, guild, storage, purchase, or other economy mutation was sent.

## P2-A4 WorldState Result

The live probe logged in, selected the lowest-level test character, entered
map 6, and safely issued one move, one normal attack, and one non-damaging
spell action.

- WorldState-applied frames: 305.
- Unique server packet types: 43.
- Monster frames: 61.
- NPC frames: 8.
- Movement frames: 52.
- Normal attack frames: 1.
- Spell frames: 1.
- Final snapshot: 127 entities, including 58 monsters and 8 NPCs.

This proves that map index/coordinates, player identity, monster/NPC model
indices, movement, attack, spell, and surrounding entity fields reach
`ZirconWorldState` on the live protocol.

## Deployment-Version Difference Fixed

The deployed server is the older Zircon Legend branch:

- `Server.MapChanged` (2013) contains one `Int32 MapIndex`.
- `Server.UserLocation` (2014) separately contains direction and coordinates.
- The current upstream branch may append an instance index to `MapChanged`.

The client now accepts both 4-byte and 8-byte `MapChanged` payloads and applies
`UserLocation` to the local player and snapshot. A map change also removes
stale non-local entities before the destination map repopulates them.

## P2-A5 Map Transition Result

The server's matching administrator command is `@MAP`; its `@MOVE` branch is
an empty implementation. The main test character performed this reversible
map sequence:

1. Start on map 1 at `(160,234)`.
2. Receive `MapChanged(map=6)` and update WorldState to map 6.
3. Receive `MapChanged(map=1)` and update WorldState back to map 1.
4. Receive `UserLocation` and finish on map 1 at `(79,149)`.

Both outbound and return transitions passed. The old server chooses a random
valid coordinate for `@MAP`, so returning to the original map intentionally
does not restore the original coordinate.

## Repeatable Tools And Regression

- `tools/Zircon.LiveWorldProbe`: authenticated safe action and map-transition probe.
- `tools/Zircon.MapTransitionInspector`: extracts real map-region transitions from `System.db`.
- `tools/Zircon.ProtocolTests`: covers deployed Chat framing, 4-byte and 8-byte MapChanged, and UserLocation.
