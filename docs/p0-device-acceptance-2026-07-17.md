# P0 Physical Device Acceptance

Date: 2026-07-17

## Passed

- Installed and cold-launched the ARM64 IL2CPP APK on Xiaomi 22041211AC / Android 14.
- Verified Chinese login and in-game button labels.
- Verified live login, character selection, world entry, movement, pickup, and skill-button flows.
- Verified normal attack end to end: the client logged `sent attack direction=6 magic=0`, followed by the server world attack response for the local character (`id=186774`).
- Captured screenshots and logcat evidence under `Builds/Android/DeviceQA`.

## Remaining

- Inspect and close the inventory, skills, NPC services, storage, and chat panels.
- Check panel navigation, scrolling where content exists, keyboard obstruction for chat, and Android back behavior.
- Complete a safe NPC interaction check without performing destructive or economic actions.
- Review final logcat and screenshots, then issue the P0 acceptance conclusion.
