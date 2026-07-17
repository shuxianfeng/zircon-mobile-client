# Phase 6 Map And Entity Rendering MVP

Date: 2026-07-08

## Current Result

Added a lightweight debug renderer:

- `Assets/Scripts/Game/World/ZirconWorldDebugRenderer.cs`

The login probe can optionally reference this component through `ZirconProtocolProbeBehaviour.worldRenderer`. When assigned in a Unity scene, it renders the current `ZirconWorldSnapshot` with generated 1x1 sprite markers:

- Local player: green
- Other player: blue
- Monster: red
- Dead monster: dim red
- NPC: yellow
- Spell/effect object: purple
- Unknown object: gray

## Purpose

This renderer is not the final art pipeline. It is a visual validation layer proving that:

- `StartGame` creates a local player snapshot.
- Server object packets update Unity-visible entity positions.
- `ObjectRemove` deletes stale markers.
- Map coordinates can be transformed into Unity world coordinates.

## Limits

- No real map tiles are rendered yet.
- No PC image library assets are converted yet.
- Marker positions use a simple `tileScale` multiplier and do not account for isometric projection, animation offsets, shadows, or draw order.
- This is intentionally safe for protocol validation and does not send movement packets by itself.

## Next Rendering Work

1. Convert a minimal PC resource sample: one map tile, one character frame, one monster frame, one NPC frame, and one item icon.
2. Replace debug markers with sprite prefabs backed by converted metadata.
3. Add camera follow for the local player.
4. Add map Y-depth sorting once real sprites are available.

## Generated Sprite Rendering

The debug renderer now has a real sprite path:

- `Assets/Scripts/Game/World/ZirconWorldDebugRenderer.cs`

It still keeps the old colored marker fallback, but now tries to load PNG frames from:

- `Assets/Generated/Textures/M-Hum`
- `Assets/Generated/Textures/Mon-1`
- `Assets/Generated/Textures/NPC`
- `Assets/Generated/Textures/MIcon`

Runtime behavior:

- Player/local player uses exported `M-Hum` frames.
- Monster uses exported `Mon-1` frames.
- NPC uses exported `NPC` frames.
- Spell/effect placeholder uses exported `MIcon` frames.
- Missing resources fall back to colored marker sprites.
- Entity `sortingOrder` is based on map Y coordinate for an initial depth-sort pass.

Added an offline preview component:

- `Assets/Scripts/Game/World/ZirconWorldPreviewBehaviour.cs`

Preview usage in Unity:

1. Create an empty scene object.
2. Add `ZirconWorldDebugRenderer`.
3. Add `ZirconWorldPreviewBehaviour`.
4. Assign the renderer field on the preview behaviour.
5. Press Play.

This validates exported PNG sprite loading without requiring a live TCP session.

## Updated Limits

- The renderer uses sample libraries rather than resolving exact model indices to the correct source library.
- Sprite pivot is bottom-center; original `.Zl` offset metadata is exported in manifests but not yet applied at runtime.
- Mobile packaging still needs a proper asset delivery path; the current loader reads from `Application.dataPath` and is intended for editor/prototype validation.

## Map Manifest Preview

Added a first map manifest path:

- `Assets/Scripts/Game/World/ZirconMapManifest.cs`
- `Assets/Scripts/Game/World/ZirconMapDebugRenderer.cs`

The asset pipeline exports `Assets/Generated/Data/Maps/0.map.manifest.json` from the real PC map file. Validation result:

- Source: `E:\codex ide\��Դ�ͻ�����Դ�ļ�\Map\0.map`
- Size: `350x350`
- Blocking cells: `34250`
- Non-empty layer cells: `31478`
- Sample cells exported: `64`

`ZirconMapDebugRenderer` loads the manifest from `Application.dataPath/Generated/Data/Maps` and renders sampled cells as translucent blocks:

- Red: blocking cell
- Blue: non-blocking cell

`ZirconWorldPreviewBehaviour` now has an optional `mapRenderer` field. Assigning it allows the offline preview scene to draw both the sampled map layer and generated entity sprites in the same coordinate space.

## Remaining Map Work

- Export viewport-sized or full map cell data instead of only sampled cells.
- Resolve map layer file/image ids to the correct `Data\Map Data\*.Zl` libraries.
- Render back/middle/front layers with PC offsets, animation frames, and draw ordering.
- Replace `Application.dataPath` prototype loading with an Android-ready asset delivery path.

## Real Tile Sample Rendering

`ZirconMapDebugRenderer` now tries to render real background map tiles before falling back to translucent debug blocks.

Current implemented mapping subset follows the PC client's `Libraries.KROrder` values:

- `0 -> Tilesc`
- `1 -> Tiles30c`
- `2 -> Tiles5c`
- `3 -> SmTilesc`
- `4 -> Housesc`
- `5 -> Cliffsc`
- `6 -> Dungeonsc`
- `7 -> Innersc`
- `8 -> Furnituresc`
- `9 -> Wallsc`
- `10 -> SmObjectsc`
- `11 -> Animationsc`
- `12 -> Object1c`
- `13 -> Object2c`

Validated first tile set:

- Source library: `E:\codex ide\开源客户端资源文件\Data\Map Data\Tiles30c.Zl`
- Exported indices: `720,721,722,723,724`
- Output folder: `Assets/Generated/Textures/MapData/Tiles30c`

The renderer loads `BackFile/BackImage` PNGs from `Assets/Generated/Textures/MapData/<Library>` and uses color blocks only when the corresponding tile image has not been exported yet.

## Camera Follow

Added local-player camera follow support:

- `Assets/Scripts/Game/World/ZirconWorldCameraFollow.cs`
- `Assets/Scripts/Game/World/ZirconWorldDebugRenderer.cs` exposes `TryGetLocalPlayerWorldPosition` and `TryGetEntityWorldPosition`.

Attach `ZirconWorldCameraFollow` to a scene object, assign the same `ZirconWorldDebugRenderer`, and optionally assign the camera. The component follows the local player marker/sprite in `LateUpdate` using a small smoothing window.

## Viewport Cell Index

`ZirconMapDebugRenderer` now builds a lookup table for loaded map cells:

- `TryGetCell(int x, int y, out ZirconMapCellManifest cell)`
- `IsBlocking(int x, int y)`

The default render limit is now `4096`, enough for the validated `48x32` viewport manifest. This allows the renderer to draw a local map window and allows input code to check loaded blocking cells before sending movement packets.
