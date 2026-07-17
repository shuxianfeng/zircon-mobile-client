# Phase 5 Asset Conversion Pipeline

Date: 2026-07-08

## Current Result

Added a standalone resource conversion tool:

- `tools/Zircon.AssetPipeline/Zircon.AssetPipeline.csproj`
- `tools/Zircon.AssetPipeline/Program.cs`
- `tools/export-zl-sample.ps1`

The tool currently supports Zircon `.Zl` image libraries used by the PC client.

Verified build:

```powershell
powershell -ExecutionPolicy Bypass -File tools\build-probe.ps1
```

Result: protocol probe and asset pipeline both build with `0 warnings` and `0 errors`.

## `.Zl` Format Notes

Source reference:

- `E:\codex ide\zircon-legend-client\Client\Models\MirLibrary.cs`

Library structure:

- `Int32 headerLength`
- `headerLength` bytes of image metadata
- Metadata payload:
  - `Int32 imageCount`
  - For each image:
    - `Boolean exists`
    - if exists:
      - `Int32 Position`
      - `Int16 Width`
      - `Int16 Height`
      - `Int16 OffSetX`
      - `Int16 OffSetY`
      - `Byte ShadowType`
      - `Int16 ShadowWidth`
      - `Int16 ShadowHeight`
      - `Int16 ShadowOffSetX`
      - `Int16 ShadowOffSetY`
      - `Int16 OverlayWidth`
      - `Int16 OverlayHeight`
- Pixel payloads are DXT1 blocks:
  - Image data at `Position`
  - Shadow data at `Position + ImageDataSize`
  - Overlay data at `Position + ImageDataSize + ShadowDataSize`

DXT1 dimensions are padded to multiples of four, matching the PC client:

```text
paddedWidth = Width + (4 - Width % 4) % 4
paddedHeight = Height + (4 - Height % 4) % 4
dataSize = paddedWidth * paddedHeight / 2
```

## Tool Behavior

The converter:

- Reads `.Zl` metadata without DirectX/SlimDX.
- Writes a JSON manifest for every image entry.
- Decodes DXT1 blocks to RGBA.
- Writes PNG samples for `image`, `shadow`, or `overlay`.
- Preserves source index, width, height, and offsets in the manifest.

Example once a real PC `Data` directory is available:

```powershell
powershell -ExecutionPolicy Bypass -File tools\export-zl-sample.ps1 `
  -Source "D:\MirClient\Data\Inventory.Zl" `
  -Output "Assets\Generated\Textures\Inventory" `
  -Type image `
  -Max 32
```

## Current Blocker

The checked PC source repo at `E:\codex ide\zircon-legend-client` does not contain actual `Data\*.Zl` resource libraries. It only contains source code and a few documentation images under `Images`.

No server or PC client changes are required. To perform visual sample conversion, provide or point this project to a real PC client resource directory containing files such as:

- `Data\Inventory.Zl`
- `Data\MIcon.Zl`
- `Data\NPC.Zl`
- `Data\M-Hum.Zl`
- `Data\Monster*.Zl`
- map `.map` files, if available

## First Sample Targets

When resource files are available, export:

1. One item icon from `Inventory.Zl` or `StoreItems.Zl`.
2. One skill icon from `MIcon.Zl`.
3. One NPC frame from `NPC.Zl`.
4. One male or female character frame from `M-Hum.Zl` / `WM-Hum.Zl`.
5. One monster frame from a `Monster*.Zl` library.

These PNGs will feed the Phase 6 placeholder renderer replacement.

## Actual Resource Validation

Resource root provided by user:

- `E:\codex ide\开源客户端资源文件`

Discovered directories:

- `Data`
- `Map`
- `Sound`
- `Translations`

Counts:

- `.Zl` libraries under `Data`: 326
- `.map` files under `Map`: 1009
- sound files under `Sound`: 1569

Validated sample exports:

| Source | Output | Exported |
| --- | --- | ---: |
| `Data\Inventory.Zl` | `Assets\Generated\Textures\Inventory` | 16 PNG + manifest |
| `Data\MIcon.Zl` | `Assets\Generated\Textures\MIcon` | 16 PNG + manifest |
| `Data\NPC.Zl` | `Assets\Generated\Textures\NPC` | 8 PNG + manifest |
| `Data\M-Hum.Zl` | `Assets\Generated\Textures\M-Hum` | 8 PNG + manifest |
| `Data\Mon-1.Zl` | `Assets\Generated\Textures\Mon-1` | 8 PNG + manifest |

PNG sanity check:

| Sample | Size | Non-transparent pixels | Unique colors |
| --- | ---: | ---: | ---: |
| `Inventory_00004_image.png` | 32x36 | 789 | 192 |
| `MIcon_00004_image.png` | 36x36 | 1296 | 230 |
| `NPC_00000_image.png` | 40x76 | 1289 | 318 |
| `Mon-1_00000_image.png` | 92x150 | 6961 | 1001 |

Conclusion: the provided resource directory is sufficient for continuing Phase 5 asset conversion and Phase 6 rendering replacement.

## Map Manifest Export

The asset pipeline now also accepts `.map` sources and writes a JSON manifest with dimensions, blocking counts, non-empty layer counts, and sampled cells.

Validated command:

```powershell
& "tools\Zircon.AssetPipeline\bin\Release\net8.0\Zircon.AssetPipeline.exe" `
  --source "E:\codex ide\开源客户端资源文件\Map\0.map" `
  --output "Assets\Generated\Data\Maps" `
  --max 64
```

Validated output:

- `Assets\Generated\Data\Maps\0.map.manifest.json`
- Size: `350x350`
- Blocking cells: `34250`
- Non-empty layer cells: `31478`
- Sample cells: `64`

## Map Tile Sample Export

PC client reference used for map layer mapping:

- `E:\codex ide\zircon-legend-client\Library\Library\Libraries.cs`
- `Libraries.KROrder[1] = LibraryFile.Tiles30c`
- `LibraryFile.Tiles30c = Data\Map Data\Tiles30c.Zl`

Validated export for the first `0.map` background sample range:

```powershell
& "tools\Zircon.AssetPipeline\bin\Release\net8.0\Zircon.AssetPipeline.exe" `
  --source "E:\codex ide\开源客户端资源文件\Data\Map Data\Tiles30c.Zl" `
  --output "Assets\Generated\Textures\MapData\Tiles30c" `
  --indices 720,721,722,723,724
```

Validated output:

- `Assets\Generated\Textures\MapData\Tiles30c\Tiles30c_00720_image.png`
- `Assets\Generated\Textures\MapData\Tiles30c\Tiles30c_00721_image.png`
- `Assets\Generated\Textures\MapData\Tiles30c\Tiles30c_00722_image.png`
- `Assets\Generated\Textures\MapData\Tiles30c\Tiles30c_00723_image.png`
- `Assets\Generated\Textures\MapData\Tiles30c\Tiles30c_00724_image.png`

All five samples are `96x64`, matching the PC client's 2x2 ground tile drawing over `48x32` map cells.

## Viewport Map Export

The `.map` exporter now supports rectangular viewport export:

```powershell
& "tools\Zircon.AssetPipeline\bin\Release\net8.0\Zircon.AssetPipeline.exe" `
  --source "E:\codex ide\开源客户端资源文件\Map\0.map" `
  --output "Assets\Generated\Data\Maps" `
  --rect 0,0,48,32
```

Validated output:

- `Assets\Generated\Data\Maps\0.map.manifest.json`
- Full map size: `350x350`
- View rectangle: `0,0,48,32`
- Exported cells: `1536`
- Global blocking cells: `34250`
- Global non-empty layer cells: `31478`

The manifest includes `ViewX`, `ViewY`, `ViewWidth`, and `ViewHeight`, and rectangular exports include empty/blocked cells as well as visual cells so the mobile client can perform local collision prechecks.
