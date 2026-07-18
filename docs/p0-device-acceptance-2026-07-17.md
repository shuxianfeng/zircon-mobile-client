# P0 Physical Device Acceptance

Date: 2026-07-17

## Passed

- Installed and cold-launched the ARM64 IL2CPP APK on Xiaomi 22041211AC / Android 14.
- Verified Chinese login and in-game button labels.
- Verified live login, character selection, world entry, movement, pickup, and skill-button flows.
- Verified normal attack end to end: the client logged `sent attack direction=6 magic=0`, followed by the server world attack response for the local character (`id=186774`).
- Captured screenshots and logcat evidence under `Builds/Android/DeviceQA`.

## Fresh Local Build And Install Verification

- Unity `2022.3.62f1c1` imported all 2,373 assets and compiled the project successfully.
- Built an ARM64 IL2CPP release APK through the ASCII-only junction
  `D:\ZirconMobileDev` because Android tooling rejects the original Chinese path.
- Latest build output: `Builds/Android/ZirconMobile.apk` (75,006,499 bytes).
- APK SHA-256:
  `E375B1C0A03C28148DFE18B45E8FA5EF3DA1D7C4D8AC860B59AC580E1F7AC87B`.
- Installed on the authorized Xiaomi 22041211AC / Android 14 device and cold
  launched `com.zircon.mobile/com.unity3d.player.UnityPlayerActivity` in 401 ms.
- The ARM64 IL2CPP process remained alive, and filtered logcat contained no
  managed fatal exception, native crash, or ANR.
- Stable launch screenshot:
  `Builds/Android/DeviceQA/zircon_launch_stable_2026-07-17.png`.
- An older `com.zircon.mobile` installation was removed with user confirmation
  because its signing certificate did not match the freshly built APK. Its old
  application data is not recoverable.

The login-state world-status issue is resolved. The status text and its
background now follow the same `LoadingMap` / `InGame` visibility boundary as
the HUD. A fresh APK was installed and cold-launched in 652 ms; PID `10707`
remained alive, the severe log filter was empty, and the clean login screen was
captured at
`Builds/Android/DeviceQA/zircon_login_overlay_fix_2026-07-17.png`.

## Acceptance Conclusion

P0 physical-device acceptance is complete. Inventory, skills, NPC services,
storage, chat, keyboard obstruction, scrolling, panel navigation, and Android
back behavior were confirmed as passed by the user from the completed
cross-device acceptance run. Together with the locally verified build, launch,
combat, pickup, and login-state overlay fix, the first playable mobile loop is
accepted and work can move to P1 live-server system validation.
