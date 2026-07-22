# P2-A2 Strict Resource Validation

Date: 2026-07-22

## Result

P2-A2 strict production-resource validation passed under the Android build
target. The validation rebuilds the production manifest and atlases before
checking the exact frame requests used by the runtime presentation code.

Validated totals:

- Sprite sets: 8.
- Exact runtime-requested frames: 168.
- AssetBundle chunks: 3.
- Atlas textures: 3.
- Atlas bounds: passed.
- Atlas rectangle overlap detection: passed.
- Android atlas format: ASTC 6x6 passed.
- `Zircon/RuntimeSprite` shader availability and support: passed.

## Exact Frame Coverage

- Female warrior body: `45000 + direction * 10 + frame`, 8 directions x 4
  frames.
- Female warrior overlay: the same 32 indices as body.
- Female warrior hair: `direction * 10 + frame`, 8 directions x 4 frames.
- Female warrior weapon: `35000 + direction * 10 + frame`, 8 directions x 4
  frames.
- Player sample: indices `0-3` and `10-13`.
- Monster sample: indices `0-3` and `1000-1003`.
- NPC sample: indices `0-3` and `100-103`.
- Effect sample: indices `0, 2, 4, ... 30`.

Each required frame must have a readable source texture, the expected bundle,
a loadable atlas, matching source/atlas dimensions, and a rectangle fully
inside the actual imported atlas texture. Rectangles sharing an atlas must not
overlap. Atlas importers must use the expected AssetBundle name and Android
ASTC 6x6 override.

## Regression

- Unity batch validation exit code: 0.
- Offline Unity UI compile check: 0 warnings and 0 errors.
- Protocol/world regression tests: 14/14 passed.

The complete official Unity 2022.3.62f1 installation was used for the batch
validation because the local 2022.3.62f1c1 executable returned a missing
runtime dependency error before opening the project. Incidental editor-version,
package-lock, generated-manifest timestamp, and atlas metadata rewrites were
restored after validation; the project remains pinned to 2022.3.62f1c1.

P2-A2 validates resource integrity and Android import/build compatibility. The
next P2-A stage is offline presentation acceptance for eight-direction player
animation, monster animation, shadows, effects, offsets, and Y sorting.
