# P2-A3 Offline Presentation Validation

Date: 2026-07-22

## Result

P2-A3 passed without a mobile device. The Android-targeted production assets
were composited through the same manifest and atlas metadata used by the
client, then validated pixel by pixel.

## Accepted Coverage

- Player: 8 directions, 4 animation frames per direction, 32 composed cells.
- Player layers: body, overlay, hair, and weapon; 128 requested layer frames.
- Weapon front/back ordering changes with direction.
- Monster: 8 animation frames with horizontal flip and shadow coverage.
- Skill effect: 16 frames.
- Player and monster shadows are non-empty and correctly offset.
- Source offsets remain inside each composed cell.
- Internal layer ordering and Y-based entity sorting pass.

## Evidence

- `docs/evidence/p2-a3/player-eight-directions-32-frames.png`
- `docs/evidence/p2-a3/monster-8-frames-with-shadows.png`
- `docs/evidence/p2-a3/skill-effect-16-frames.png`
- `docs/evidence/p2-a3/y-sorting-overlap.png`
- `docs/evidence/p2-a3/validation-summary.json`

The repeatable Unity entry point is
`Zircon.Mobile.Editor.ZirconP2A3OfflinePresentationValidation.RunBatch`.
