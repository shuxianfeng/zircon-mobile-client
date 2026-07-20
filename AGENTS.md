# Zircon Mobile Workspace Instructions

## Server network profiles

- When the user says they are **at home / 在家**, use `192.168.0.100:17000`.
- When the user says they are **at the office / 在公司**, use `zircon.35861344.xyz:17000`.
- Do not ask the user to repeat these endpoints after their location is known.
- Prefer `tools/probe-server-profile.ps1` for safe handshake checks.
- Do not send economy, mail, market, guild, trade, item-consumption, or other
  persistent mutations without explicit authorization.

These profiles are durable project configuration. The Unity scene may retain
the home endpoint between builds; apply the correct endpoint for the stated
location before a live build or protocol test.

