# Phase 4 Shared Model Migration

Date: 2026-07-08

## Scope Completed

This phase starts the mobile-side model layer without changing the PC client or the server.

Implemented Unity-side model files:

- `Assets/Scripts/Game/Entities/ZirconEntityKind.cs`
- `Assets/Scripts/Game/Entities/ZirconEntityState.cs`
- `Assets/Scripts/Game/World/ZirconWorldState.cs`
- `Assets/Scripts/Game/World/ZirconWorldSnapshot.cs`
- `Assets/Scripts/Game/World/ZirconChatLogEntry.cs`

The world state currently consumes the protocol packets already verified against the live server:

- `Server.StartGame` 2012
- `Server.ObjectMove` 2019
- `Server.ObjectRemove` 2015
- `Server.ObjectTurn` 2016
- `Server.ObjectMonster` 2035
- `Server.ObjectNpc` 2036
- `Server.ObjectSpell` 2038
- `Server.DataObjectPlayer` 2166
- `Server.DataObjectMonster` 2167
- `Server.DataObjectLocation` 2169
- `Server.DataObjectMaxHealthMana` 2171
- `Server.GoldChanged` 2067
- `Server.Chat` 2069
- `Server.GameGoldChanged` 2111
- `Server.WeightUpdate` 2114
- `Server.HuntGoldChanged` 2115
- `Server.StatsUpdate` 2051
- `Server.AutoTimeChanged` 2184
- `Server.SkillConfig` 2189

## Model Decisions

- `ZirconMapPoint` replaces desktop `System.Drawing.Point` for protocol-facing map coordinates.
- Runtime entity state is separated from packet DTOs so rendering and UI can evolve without changing packet decoders.
- `ZirconWorldState` is thread-safe because packets arrive from the TCP receive loop while Unity UI reads snapshots on the main thread.
- Unknown object packets are retained as `ZirconEntityKind.Unknown` and upgraded when a later packet identifies the type.
- Chat is retained as a bounded in-memory list for the login prototype and HUD prototype.
- Currency, weight, auto-time, and skill level limit are retained as simple snapshot fields for the first HUD prototype.

## Unity Probe Integration

`ZirconProtocolProbeBehaviour` now:

- Displays a live world summary after `StartGame`.
- Feeds decoded in-game packets into `ZirconWorldState`.
- Flushes network logs on `Update()` so background packet events appear in the IMGUI panel.

## Current Limitations

- The model layer does not yet decode inventory, equipment, buffs, skills, NPC dialogs, item drops, or map tile data.
- Health and mana fields are only populated from the verified packets currently decoded.
- This is still a protocol/model prototype; no entity renderer or map renderer has been attached yet.
- Local Unity compilation has not been verified on this machine because no .NET SDK/Unity build process is available in the shell environment.

## Next Work

1. Add outgoing movement/chat packet encoders after field order is verified from the PC packet schema.
2. Decode the next packet batch observed after `StartGame`: weight, skill config, gold, buff, and object removal packets.
3. Add a lightweight in-scene world visualizer that renders placeholder sprites from `ZirconWorldSnapshot`.
4. Begin Phase 5 asset sample conversion once a concrete PC resource path and target sample set are selected.


