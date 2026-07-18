# P1-A Live Server Acceptance

Date: 2026-07-18

## Result

P1-A is complete. Chat routing, MagicKey persistence, and the Buff lifecycle
were validated on the physical Android device against the live server.

The final client is configured for `192.168.0.100:17000` with IPv6 preference
disabled. The ARM64 IL2CPP APK was installed and cold-launched successfully.
Its SHA-256 is
`A73EB9E27E32E78BB0B203E1B0C89FAF29F38FBC37262BB125FE3FA44438393D`.

## Chat

- Local chat sent and received `P1A_CHAT_20260717_001`.
- Shout and global messages received the expected server routing types.
- Self-whisper produced both the sent-message record and the incoming whisper.
- The generated chat panel now contains LOCAL, GROUP, GUILD, SHOUT, GLOBAL,
  and WHISPER buttons plus a conditional player-name field.
- GLOBAL and WHISPER button tests proved that the client adds `!@` and
  `/name ` automatically; users no longer need to type protocol prefixes.
- The selected channel remains clickable and is shown with an ASCII `*` prefix
  instead of a disabled-looking grey state. Final physical-device UI acceptance
  passed.

Environmental exceptions:

- Group payload `!!P1A_GROUP_001` was sent, but the character was not in a
  group, so no group echo was expected.
- The server treated guild payload `!~P1A_GUILD_001` as shout text containing
  the remaining `~`. The mobile packet contained the documented PC-compatible
  prefix, so this is recorded as a server/runtime compatibility finding; no
  server change was made.

## MagicKey

- Bound Basic Swordsmanship to mobile hotbar position 1.
- Sent MagicKey packet 1045 with `info=1`, `type=100`, keys `1/0/0/0`.
- The local skill panel updated immediately.
- After fully terminating the application and logging in through a new process,
  StartGame restored Basic Swordsmanship in position 1. Server persistence
  passed.

## Buff Lifecycle

- Bound Iron Shirt to hotbar position 2 and cast MagicType 111.
- The server returned BuffAdd packet 2085; the client added `type=100` and the
  Buff bar increased from three entries to four with a visible countdown.
- The same Buff index was removed by packet 2086 after natural expiry, and the
  UI removed the entry.
- No managed fatal exception, native crash, or ANR was recorded.

## LAN Login Evidence

- Connection state completed `Connecting -> Connected -> LoggingIn`.
- Login returned three characters.
- Character `翻云覆雨` entered the world successfully.
- StartGame initialized map 1 with 26 items, 22 skills, and 3 persistent Buffs.

## Next Work

Proceed to P1-B with a disposable test character: inventory/equipment
mutations, NPC buy/sell/repair, and storage operations. These tests change
server-side character data and require explicit authorization before execution.
