# P2-A Company Server Connectivity

Date: 2026-07-20

## Durable endpoint profiles

- Home: `192.168.0.100:17000`.
- Company: `zircon.35861344.xyz:17000`.

The company endpoint is currently IPv6-only. Public DNS returned:

`2408:8244:510:cf3:639a:c387:b3a6:c3aa`

The address may change and must be resolved from the hostname rather than
stored as permanent configuration. The office DNS resolver returned the zone
SOA without an AAAA answer, while AliDNS `223.5.5.5` and Google DNS `8.8.8.8`
both returned the IPv6 address. `tools/probe-company-server.ps1` implements the
public-DNS fallback.

## Safe handshake result

- IPv6 TCP `17000`: passed.
- Server `General.Connected` packet 1: received.
- Client Connected response: sent.
- Server `General.GoodVersion` packet 5: received.
- Chinese language selection: sent.
- Server Ping packet 2 and PingResponse packet 6: received.
- Server CheckClientHash packet 2180: received.
- Final protocol state: `ReadyForLogin`.

No email, password, character selection, gameplay command, or persistent
mutation was sent. Authenticated P2 entity/action field capture remains a
separate gate requiring securely supplied test credentials or a manual login.

