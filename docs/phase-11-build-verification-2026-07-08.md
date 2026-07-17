# Phase 11 Build Verification

Date: 2026-07-08

## Local SDK

The project now has a local .NET SDK installed at:

- `.tools/dotnet`

Installed SDK:

- .NET SDK `8.0.422`

The SDK is project-local and does not require changing machine-wide `PATH`.

## Build Scripts

Build both standalone tools:

```powershell
powershell -ExecutionPolicy Bypass -File tools\build-probe.ps1 -Configuration Release
```

Install or repair the local SDK:

```powershell
powershell -ExecutionPolicy Bypass -File tools\install-dotnet-sdk.ps1 -Channel 8.0 -InstallDir .tools\dotnet
```

Export `.Zl` samples once a real PC resource directory is available:

```powershell
powershell -ExecutionPolicy Bypass -File tools\export-zl-sample.ps1 `
  -Source "D:\MirClient\Data\Inventory.Zl" `
  -Output "Assets\Generated\Textures\Inventory" `
  -Type image `
  -Max 32
```

## Verified Result

Release build result:

- `tools/Zircon.ProtocolProbe`: `0 warnings`, `0 errors`
- `tools/Zircon.AssetPipeline`: `0 warnings`, `0 errors`

This verifies:

- Core protocol code compiles outside Unity.
- The standalone TCP protocol probe compiles.
- The `.Zl` metadata/DXT1/PNG asset pipeline compiles.

## Not Verified In This Pass

- Unity Editor import/build, because the Unity editor was not launched from this shell.
- Live server TCP validation, because it is not needed for this local toolchain step.
- Real `.Zl` image export, because the checked PC source repo does not contain actual `Data\*.Zl` resource files.
