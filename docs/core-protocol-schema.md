# Core Protocol Schema

This document freezes the first packet schemas used by the mobile prototype. It is intentionally narrow and will be expanded after packet capture and login validation.

## Frame

All packets:

| Offset | Type | Meaning |
| --- | --- | --- |
| 0 | `Int32` little-endian | Total packet length, including these 4 bytes |
| 4 | `UInt16` little-endian | Packet ID |
| 6 | bytes | Payload |

Primitive encoding follows .NET `BinaryWriter`/`BinaryReader` behavior used by the PC client:

- `string`: `BinaryWriter.Write(string)` UTF-8 with 7-bit encoded byte length.
- `byte[]`: `Int32 length` followed by raw bytes.
- `DateTime`: `Int64 DateTime.ToBinary()`.
- `TimeSpan`: `Int64 ticks`.
- `System.Drawing.Point`: `Int32 X`, `Int32 Y`.
- `System.Drawing.Size`: `Int32 Width`, `Int32 Height`.
- `System.Drawing.Color`: `Int32 ToArgb()`.
- Enums: written using declared underlying type.
- `List<T>`: `Int32 count`, then elements.
- Class references: `Boolean hasValue`, then object fields if true.

## General Packets

| ID | Direction | Name | Fields |
| --- | --- | --- | --- |
| 1 | both | `Connected` | none |
| 2 | both | `Ping` | none |
| 3 | server to client | `CheckVersion` | none |
| 4 | client to server | `Version` | `Byte[] ClientHash` |
| 5 | server to client | `GoodVersion` | none |
| 6 | server to client | `PingResponse` | `Int32 Ping` |
| 7 | both | `Disconnect` | `DisconnectReason Reason` as byte |

`DisconnectReason` byte values:

| Value | Name |
| --- | --- |
| 0 | Unknown |
| 1 | TimedOut |
| 2 | WrongVersion |
| 3 | ServerClosing |
| 4 | AnotherUser |
| 5 | AnotherUserPassword |
| 6 | AnotherUserAdmin |
| 7 | Banned |
| 8 | Crashed |

## Client Login Packets

| ID | Name | Fixed field order |
| --- | --- | --- |
| 1007 | `SelectLanguage` | `String Language` |
| 1008 | `Login` | `String EMailAddress`, `String Password`, `String CheckSum` |
| 1012 | `StartGame` | `Int32 CharacterIndex` |
| 1109 | `CheckClientDb` | `String Hash` |
| 1111 | `LoginSimple` | `String EMailAddress`, `String Password`, `String CheckSum` |

Password behavior:

- For typed passwords, PC sends `MD5(email + "-" + plaintextPassword)` as a lowercase hex string.
- Remembered passwords are already stored as the hash string and sent directly.

CheckSum behavior:

- PC creates or reads a random string from `Documents\Zircon\CheckSum.bin`.
- The mobile probe defaults to a random 20-character value, with `--checksum` override.

## Server Login Packets

| ID | Name | Known top-level field order |
| --- | --- | --- |
| 2003 | `Login` | `LoginResult Result`, `String Message`, `TimeSpan Duration`, `List<SelectInfo> Characters`, `List<ClientUserItem> Items`, `List<ClientBlockInfo> BlockList`, `String Address`, `Boolean TestServer` |
| 2012 | `StartGame` | `StartGameResult Result`, `String Message`, `TimeSpan Duration`, `StartInformation StartInformation` |
| 2179 | `CheckClientDb` | `Boolean IsUpgrading`, `Int32 CurrentIndex`, `Int32 TotalCount`, `Byte[] Datas` |
| 2180 | `CheckClientHash` | `List<ClientUpgradeItem> ClientFileHash` |
| 2182 | `LoginSimple` | `LoginResult Result`, `String Message`, `TimeSpan Duration`, `List<SelectInfo> Characters`, `String Address`, `Boolean TestServer` |
| 2183 | `AccountExpand` | `List<ClientUserItem> Items`, `List<ClientBlockInfo> BlockList` |

`LoginResult` byte values:

| Value | Name |
| --- | --- |
| 0 | Disabled |
| 1 | BadEMail |
| 2 | BadPassword |
| 3 | AccountNotExists |
| 4 | AccountNotActivated |
| 5 | WrongPassword |
| 6 | Banned |
| 7 | AlreadyLoggedIn |
| 8 | AlreadyLoggedInPassword |
| 9 | AlreadyLoggedInAdmin |
| 10 | Success |

`StartGameResult` byte values:

| Value | Name |
| --- | --- |
| 0 | Disabled |
| 1 | Deleted |
| 2 | Delayed |
| 3 | UnableToSpawn |
| 4 | NotFound |
| 5 | Success |

## Open Risks

- Server may require the exact PC executable MD5 in `Version.ClientHash`.
- Server may require DB hash or client hash follow-up before allowing login.
- Reflection property order must be confirmed through packet capture or a matching PC build runtime.
- Nested models such as `ClientUserItem`, `Stats`, and `StartInformation` need Phase 3 schema expansion before full game entry can be decoded safely.
