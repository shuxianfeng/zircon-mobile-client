# Phase 8 Formal Mobile UI

Date: 2026-07-14

## Implemented Controllers

- ZirconLoginPanelBehaviour validates account fields, submits login, and displays connection state.
- ZirconCharacterSelectPanelBehaviour creates one uGUI button per server character and starts the selected character.
- ZirconUiFlowBehaviour switches between login, character selection, and in-game roots.
- ZirconMainHudBehaviour binds character name, level, HP, MP, currency, map, and coordinates.
- ZirconMobileSkillButtonBehaviour provides a first target-aware skill button with cooldown overlay and text.

ZirconProtocolProbeBehaviour now exposes reusable session events and methods for these controllers. Its IMGUI overlay and automatic connection are disabled by default, preserving it as an optional protocol diagnostic instead of the product UI.

## Remaining Acceptance

1. Create or update the Unity scene and prefabs with the controller references.
2. Import the selected skill icon from the generated Magic asset library.
3. Verify safe-area layout on representative Android aspect ratios.
4. Run Unity compilation without opening a visible game window.
5. Restore IPv6 access and validate login, selection, HUD updates, one skill cast, and one real ground-item pickup.
6. Continue with inventory, equipment, NPC dialog, and Android build work.