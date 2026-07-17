# Phase 1 StartGame Validation

Date: 2026-07-08

## Target

- Host: `zircon.35861344.xyz`
- Address: `2408:8244:510:17a4:9c31:aec8:e3c8:871b`
- Port: `17000`
- Probe: `tools\probe-startgame.ps1`

## Result Summary

The mobile-side protocol probe successfully completed:

1. IPv6 TCP connection.
2. `General.Connected` handshake.
3. `General.GoodVersion` acknowledgement.
4. `SelectLanguage(1007)`.
5. `Login(1008)` with real test account.
6. `Server.Login(2003)` success.
7. Character list decoding.
8. `StartGame(1012)` for the first character.
9. `Server.StartGame(2012)` success.
10. First in-game packet batch recording.

No server or PC client code was modified.

## Character List

Decoded `Server.Login(2003)`:

```text
decode Login result=10 message='' characters=3
character index=1 name=翻云覆雨 level=100 gender=1 class=0 location=1
character index=2 name=上善若水 level=76 gender=0 class=2 location=66
character index=3 name=魔王 level=32 gender=0 class=1 location=6
```

`LoginResult=10` maps to `Success`.

## StartGame

The probe selected the first character:

```text
send StartGame characterIndex=1 hex=0A 00 00 00 F4 03 01 00 00 00
```

Server response:

```text
recv id=2012 len=3876 ...
decode StartGame result=5 message='' payloadLength=3870
```

`StartGameResult=5` maps to `Success`.

## First In-Game Packet Batch

Representative packet IDs received after entering game:

| Packet ID | PC packet name | Meaning |
| --- | --- | --- |
| 2069 | `ServerPackets.Chat` | System/player chat messages. |
| 2189 | `ServerPackets.SkillConfig` | Skill level configuration. |
| 2051 | `ServerPackets.StatsUpdate` | Player stats sync. |
| 2114 | `ServerPackets.WeightUpdate` | Bag/wear/hand weight. |
| 2166 | `ServerPackets.DataObjectPlayer` | Big-map/minimap player data object. |
| 2035 | `ServerPackets.ObjectMonster` | Monster object spawn/show. |
| 2167 | `ServerPackets.DataObjectMonster` | Map data object for monster. |
| 2036 | `ServerPackets.ObjectNPC` | NPC object spawn/show. |
| 2038 | `ServerPackets.ObjectSpell` | Static spell/effect object. |
| 2042 | `ServerPackets.MagicToggle` | Skill availability toggle. |
| 2097 | `ServerPackets.MarketPlaceConsign` | Marketplace state. |
| 2104 | `ServerPackets.MailList` | Mail list state. |
| 2111 | `ServerPackets.GameGoldChanged` | Game gold state. |
| 2115 | `ServerPackets.HuntGoldChanged` | Hunt gold state. |
| 2184 | `ServerPackets.AutoTimeChanged` | Auto-time state. |
| 2150 | `ServerPackets.ReviveTimers` | Revive cooldown state. |
| 2156 | `ServerPackets.CompanionWeightUpdate` | Companion inventory/weight. |
| 2161 | `ServerPackets.MarriageInfo` | Partner info. |
| 2085 | `ServerPackets.BuffAdd` | Buff state. |
| 2171 | `ServerPackets.DataObjectMaxHealthMana` | Data object max HP/MP. |
| 2169 | `ServerPackets.DataObjectLocation` | Map object location updates. |
| 2019 | `ServerPackets.ObjectMove` | Object movement. |
| 2016 | `ServerPackets.ObjectTurn` | Object turning. |
| 2174 | `ServerPackets.HelmetToggle` | Helmet display state. |
| 2147 | `ServerPackets.GuildCastleInfo` | Castle info. |
| 2177 | `ServerPackets.FortuneUpdate` | Fortune state. |
| 2188 | `ServerPackets.WeaponRefineBase` | Weapon refine config. |

## Conclusion

The first milestone `Mobile Protocol Login Prototype` is technically achieved:

- The mobile-side client can connect to the online IPv6 service.
- The handshake is compatible.
- The login packet is compatible.
- Character list decoding is proven.
- Character selection / StartGame is accepted.
- The server streams real in-game map/entity/state packets.

## Next Engineering Work

1. Harden packet schema for login, character list, StartGame, map object, chat, stats, and movement packets.
2. Implement stronger decoders in `Assets/Scripts/Core/Protocol` for the representative first-batch packets.
3. Start the Unity project skeleton and connect it to the protocol layer.
4. Build a placeholder login/character/game-state UI that displays connection, login, character list, and packet stream state.
