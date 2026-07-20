# P2-A Authenticated Protocol Probe

Date: 2026-07-20

## Scope

The company endpoint `zircon.35861344.xyz:17000` was tested without a mobile
device. Credentials were supplied out of band to the probe and are not stored
in this document or in project configuration. Raw packet hex logging was
disabled.

## Results

- IPv6 TCP and version negotiation: passed.
- Standard login packet (`Client.Login`, 1008): server returned
  `LoginResult.Disabled`; no characters were returned.
- Simple login packet (`Client.LoginSimple`, 1111): server returned the same
  `LoginResult.Disabled`; no characters were returned.
- The result is distinct from `AccountNotExists`, `WrongPassword`, `Banned`,
  and `AlreadyLoggedIn` in the shared protocol enum.
- No `StartGame`, movement, attack, spell, NPC, inventory, economy, or other
  gameplay mutation was sent.

## Gate

The server is reachable and protocol-compatible through the login response,
but authenticated P2 field capture is blocked while the server login switch is
disabled. After server login is enabled, rerun the probe to:

1. Read the character list.
2. Start a disposable test character.
3. Capture map, entity model, movement, normal attack, spell, and map-transition
   fields without economy mutations.

Android installation and visual/performance acceptance remain separate gates
and require the physical device later.
