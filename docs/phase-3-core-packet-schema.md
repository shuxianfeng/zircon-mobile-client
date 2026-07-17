# Phase 3 Core Packet Schema

Date: 2026-07-08

## Scope

This schema covers the first packets proven by the online IPv6 `StartGame` validation. It intentionally focuses on fields needed by the mobile MVP and avoids desktop-only rendering state.

Source reference:

- `E:\codex ide\zircon-legend-client\Library\Library\Network\ServerPackets.cs`
- `E:\codex ide\zircon-legend-client\Library\Library\Globals.cs`
- `E:\codex ide\zircon-legend-client\Library\Library\Stat.cs`

Mobile implementation:

- `Assets/Scripts/Core/Protocol/ZirconServerPacketDecoder.cs`
- `Assets/Scripts/Core/Protocol/ZirconInGamePacketDecoder.cs`
- `Assets/Scripts/Core/Protocol/ZirconStatusPacketDecoder.cs`

## Primitive Rules

- Frame: `Int32 length`, `UInt16 packetId`, payload.
- `Point`: `Int32 X`, `Int32 Y`.
- `Color`: `Int32 ARGB`.
- `TimeSpan`: `Int64 ticks`.
- `DateTime`: `Int64 ToBinary`.
- `Stats`: `Int32 count`, then repeated `Int32 stat`, `Int32 value`.
- Class properties, including `Stats` and `List<T>`, are preceded by `Boolean hasValue`.

## Login And Start

| ID | Name | Fixed field order |
| --- | --- | --- |
| 2003 | `Server.Login` | `LoginResult byte`, `String Message`, `TimeSpan Duration`, `List<SelectInfo> Characters`, `List<ClientUserItem> Items`, `List<ClientBlockInfo> BlockList`, `String Address`, `Boolean TestServer` |
| 2012 | `Server.StartGame` | `StartGameResult byte`, `String Message`, `TimeSpan Duration`, `StartInformation StartInformation` |

`SelectInfo`:

| Field | Type |
| --- | --- |
| CharacterIndex | `Int32` |
| CharacterName | `String` |
| Level | `Int32` |
| Gender | `Byte` |
| Class | `Byte` |
| Location | `Int32` |
| LastLogin | `DateTime Int64` |

`StartInformation` mobile-decoded prefix:

| Field | Type |
| --- | --- |
| Index | `Int32` |
| ObjectID | `UInt32` |
| Name | `String` |
| NameColour | `Int32 ARGB` |
| GuildName | `String` |
| GuildRank | `String` |
| Class | `Byte` |
| Gender | `Byte` |
| Location | `Point` |
| Direction | `Byte` |
| MapIndex | `Int32` |
| Gold | `Int64` |
| GameGold | `Int32` |
| Level | `Int32` |
| HairType | `Int32` |
| HairColour | `Int32 ARGB` |
| Weapon | `Int32` |
| Armour | `Int32` |
| Shield | `Int32` |
| ArmourColour | `Int32 ARGB` |
| ArmourImage | `Int32` |
| Experience | `Decimal` |
| CurrentHP | `Int32` |
| CurrentMP | `Int32` |

The remaining `StartInformation` fields include modes, inventory, belt links, magic, buffs, poison, safe-zone state, mount state, quests, companions, storage size, and auto-fight links. They are pending Phase 4 model migration.

## Movement And Objects

| ID | Name | Fixed field order |
| --- | --- | --- |
| 2016 | `ObjectTurn` | `UInt32 ObjectID`, `Byte Direction`, `Point Location`, `TimeSpan Slow` |
| 2019 | `ObjectMove` | `UInt32 ObjectID`, `Byte Direction`, `Point Location`, `Int32 Distance`, `TimeSpan Slow` |
| 2022 | `ObjectAttack` | `UInt32 ObjectID`, `Byte Direction`, `Point Location`, `MagicType Int32`, `Element Byte`, `UInt32 TargetID`, `TimeSpan Slow` |
| 2035 | `ObjectMonster` | `UInt32 ObjectID`, `Int32 MonsterIndex`, `Int32 NameColour`, `String PetOwner`, `Byte Direction`, `Point Location`, `Boolean Dead`, `Boolean Skeleton`, `PoisonType Int32`, event booleans, buffs, extra companion data |
| 2036 | `ObjectNPC` | `UInt32 ObjectID`, `Int32 NPCIndex`, `Point CurrentLocation`, `Byte Direction` |
| 2038 | `ObjectSpell` | `UInt32 ObjectID`, `Byte Direction`, `Point Location`, `SpellEffect Int32`, `Int32 Power` |

## Data Objects

| ID | Name | Fixed field order |
| --- | --- | --- |
| 2166 | `DataObjectPlayer` | `UInt32 ObjectID`, `Int32 MapIndex`, `Point CurrentLocation`, `String Name`, `Int32 Health`, `Int32 Mana`, `Boolean Dead`, `Int32 MaxHealth`, `Int32 MaxMana` |
| 2167 | `DataObjectMonster` | `UInt32 ObjectID`, `Int32 MapIndex`, `Point CurrentLocation`, `Int32 MonsterIndex`, `String PetOwner`, `Int32 Health`, `Stats Stats`, `Boolean Dead` |
| 2169 | `DataObjectLocation` | `UInt32 ObjectID`, `Int32 MapIndex`, `Point CurrentLocation` |
| 2171 | `DataObjectMaxHealthMana` | `UInt32 ObjectID`, `Int32 MaxHealth`, `Int32 MaxMana`, nullable `Stats Stats` |


## Client Action Packets

| ID | Name | Fixed field order |
| --- | --- | --- |
| 1014 | `Client.Turn` | `MirDirection Byte Direction` |
| 1016 | Client.Move | MirDirection Byte Direction, Int32 Distance |
| 1018 | Client.Attack | MirDirection Byte Direction, MirAction Byte Action, MagicType Int32 AttackMagic |
| 1029 | `Client.PickUp` | `Byte PickType` |
| 1030 | `Client.Chat` | `String Text` |
| 1031 | `Client.NPCCall` | `UInt32 ObjectID` |

These packets are now encoded by `ZirconClientPackets`, exposed through `ZirconNetworkClient`, and available in `ZirconProtocolProbeBehaviour` through manual Turn, Move, PickUp, and Chat buttons once the client is in game.
## HUD And Chat

| ID | Name | Fixed field order |
| --- | --- | --- |
| 2051 | `StatsUpdate` | nullable `Stats Stats`, nullable `Stats HermitStats`, `Int32 HermitPoints` |
| 2067 | `GoldChanged` | `Int64 Gold` |
| 2069 | `Chat` | `UInt32 ObjectID`, `String Text`, `MessageType Int32`, `List<ClientUserItem> Items` |
| 2111 | `GameGoldChanged` | `Int32 GameGold` |
| 2114 | `WeightUpdate` | `Int32 BagWeight`, `Int32 WearWeight`, `Int32 HandWeight` |
| 2115 | `HuntGoldChanged` | `Int32 HuntGold` |
| 2184 | `AutoTimeChanged` | `Int64 AutoTime` |
| 2189 | `SkillConfig` | `Int32 SkillLevelLimit` |

## Decoder Policy

- Known packets decode into small Unity-safe DTOs.
- Unknown packets remain logged by ID/length/hex and are ignored.
- Complex nested objects such as `ClientUserItem`, inventory, magic, buffs, quests, companions, and market/mail payloads are not fully decoded yet.
- Packet decode failures return `false` and must not crash the client.

## Next Schema Targets

1. `ClientUserItem` and inventory lists.
2. `ClientUserMagic` and skill list.
3. Buff payloads: `BuffAdd`, `BuffChanged`, `BuffPaused`.
4. NPC interaction packets: `NPCResponse`, `NPCButton`, `NPCCall`.
5. Server correction packets, object removal variants, and skill/buff payload details.





## NPC Interaction

| ID | Name | Fixed field order |
| --- | --- | --- |
| 2070 | `NPCResponse` | `UInt32 ObjectID`, `Int32 PageIndex`, `List<ClientRefineInfo> Extra` |
| 2076 | `NPCClose` | no payload |

The mobile MVP currently decodes the stable NPCResponse prefix. Page content is resolved from client NPC page data rather than transmitted as text by this packet.